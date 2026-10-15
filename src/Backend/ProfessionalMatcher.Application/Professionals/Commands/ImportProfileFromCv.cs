using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.CvImport;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Exceptions;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Fills a profile from a CV in PDF: general data, skills, experiences and education. Nothing already on the
/// profile is removed or overwritten (unless asked for general data), and entries that are already there are skipped.
/// </summary>
public class ImportProfileFromCv
{
    public const int MaxSizeInBytes = 5 * 1024 * 1024;

    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();

    public class Handler(
        IProfessionalRepository professionals,
        ISkillRepository skills,
        IExperienceRepository experiences,
        IEducationRepository education,
        IPdfTextExtractor pdfTextExtractor,
        ProfessionalModelAssembler assembler)
    {
        /// <returns>The import result, or null if the professional was not found.</returns>
        /// <exception cref="UnreadableDocumentException">The file is not a readable PDF, or nothing of a CV was recognised in it.</exception>
        public async Task<CvImportModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            var professional = await professionals.GetById(request.ProfessionalId, cancellationToken);
            if (professional is null) return null;

            using var stream = new MemoryStream(request.Content);
            var parsed = CvTextParser.Parse(pdfTextExtractor.ExtractText(stream), DateOnly.FromDateTime(DateTime.UtcNow));

            if (parsed.Skills.Count == 0 && parsed.Experiences.Count == 0 && parsed.Education.Count == 0)
                throw new UnreadableDocumentException(
                    "No CV content was recognised in this PDF. Expected sections such as Experience, Technical Skills and Education.");

            var summary = new CvImportSummary { IgnoredSections = parsed.IgnoredSections };

            var updatedDetails = ApplyDetails(professional, parsed, request.OverwriteDetails);
            if (updatedDetails.Count > 0) await professionals.Replace(professional, cancellationToken);
            summary.UpdatedDetails = updatedDetails;

            await ImportSkills(professional.Id, parsed.Skills, summary, cancellationToken);
            await ImportExperiences(professional.Id, parsed.Experiences, summary, cancellationToken);
            await ImportEducation(professional.Id, parsed.Education, summary, cancellationToken);
            summary.Warnings = parsed.Warnings;

            return new CvImportModel { Profile = await assembler.Assemble(professional, cancellationToken), Summary = summary };
        }

        private static List<string> ApplyDetails(Professional professional, ParsedCv parsed, bool overwrite)
        {
            var updated = new List<string>();

            void Fill(string field, string? parsedValue, string? current, int maxLength, Action<string> set)
            {
                if (string.IsNullOrWhiteSpace(parsedValue)) return;
                if (!overwrite && !string.IsNullOrWhiteSpace(current)) return;

                var value = Truncate(parsedValue.Trim(), maxLength);
                if (value == current) return;
                set(value);
                updated.Add(field);
            }

            // The e-mail is never touched: it identifies the account and must stay unique.
            if (overwrite) Fill("fullName", parsed.FullName, professional.FullName, 200, v => professional.FullName = v);
            Fill("headline", parsed.Headline, professional.Headline, 200, v => professional.Headline = v);
            Fill("location", parsed.Location, professional.Location, 200, v => professional.Location = v);
            Fill("summary", parsed.Summary, professional.Summary, 4000, v => professional.Summary = v);
            return updated;
        }

        private async Task ImportSkills(Guid professionalId, IReadOnlyList<ParsedSkill> parsed, CvImportSummary summary, CancellationToken ct)
        {
            var known = (await skills.ListByProfessionalId(professionalId, ct))
                .Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in parsed)
            {
                var name = Truncate(item.Name, 100);
                if (!known.Add(name))
                {
                    summary.SkillsSkipped++;
                    continue;
                }

                await skills.Add(new Skill
                {
                    ProfessionalId = professionalId,
                    Name = name,
                    Category = Truncate(item.Category, 100),
                    YearsOfExperience = Math.Clamp(item.YearsOfExperience, 0, 70),
                    Level = Math.Clamp(item.Level, 1, 5),
                    IsPrimary = item.IsPrimary,
                }, ct);
                summary.SkillsAdded++;
            }
        }

        private async Task ImportExperiences(Guid professionalId, IReadOnlyList<ParsedExperience> parsed, CvImportSummary summary, CancellationToken ct)
        {
            var known = (await experiences.ListByProfessionalId(professionalId, ct))
                .Select(e => ExperienceKey(e.Company, e.Role, e.StartDate)).ToHashSet();

            foreach (var item in parsed)
            {
                var company = Truncate(item.Company, 200);
                var role = Truncate(item.Role, 200);
                if (!known.Add(ExperienceKey(company, role, item.StartDate)))
                {
                    summary.ExperiencesSkipped++;
                    continue;
                }

                await experiences.Add(new Experience
                {
                    ProfessionalId = professionalId,
                    Company = company,
                    Role = role,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    Description = item.Description,
                    Technologies = [.. item.Technologies.Select(t => Truncate(t, 100))],
                }, ct);
                summary.ExperiencesAdded++;
            }
        }

        private async Task ImportEducation(Guid professionalId, IReadOnlyList<ParsedEducation> parsed, CvImportSummary summary, CancellationToken ct)
        {
            var known = (await education.ListByProfessionalId(professionalId, ct))
                .Select(e => EducationKey(e.Institution, e.Degree, e.StartYear)).ToHashSet();

            foreach (var item in parsed)
            {
                var institution = Truncate(item.Institution, 200);
                var degree = Truncate(item.Degree, 200);
                if (!known.Add(EducationKey(institution, degree, item.StartYear)))
                {
                    summary.EducationSkipped++;
                    continue;
                }

                await education.Add(new Education
                {
                    ProfessionalId = professionalId,
                    Institution = institution,
                    Degree = degree,
                    StartYear = item.StartYear,
                    EndYear = item.EndYear,
                }, ct);
                summary.EducationAdded++;
            }
        }

        private static (string, string, DateOnly) ExperienceKey(string company, string role, DateOnly start) =>
            (company.Trim().ToLowerInvariant(), role.Trim().ToLowerInvariant(), start);

        private static (string, string, int) EducationKey(string institution, string degree, int startYear) =>
            (institution.Trim().ToLowerInvariant(), degree.Trim().ToLowerInvariant(), startYear);

        private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max].TrimEnd();
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public byte[] Content { get; set; } = [];

        /// <summary>When true, name, headline, location and summary are replaced by the CV's; by default only blanks are filled.</summary>
        public bool OverwriteDetails { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().WithMessage("The file is empty.");
            RuleFor(x => x.Content).Must(c => c.Length <= MaxSizeInBytes)
                .WithMessage($"The PDF must be at most {MaxSizeInBytes / 1024 / 1024} MB.");
            RuleFor(x => x.Content).Must(c => c.AsSpan().StartsWith(PdfMagic))
                .When(x => x.Content.Length > 0)
                .WithMessage("The file is not a PDF.");
        }
    }
}
