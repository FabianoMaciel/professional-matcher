using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Application.Models;

public class ProfessionalModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Headline { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public IReadOnlyList<SkillModel> Skills { get; set; } = [];
    public IReadOnlyList<ExperienceModel> Experiences { get; set; } = [];
    public IReadOnlyList<EducationModel> Education { get; set; } = [];

    // Manual mapping (no AutoMapper): one explicit conversion per model.
    public static ProfessionalModel From(
        Professional professional,
        IEnumerable<Skill> skills,
        IEnumerable<Experience> experiences,
        IEnumerable<Education> education) => new()
    {
        Id = professional.Id,
        FullName = professional.FullName,
        Email = professional.Email,
        Headline = professional.Headline ?? string.Empty,
        Location = professional.Location ?? string.Empty,
        Summary = professional.Summary ?? string.Empty,
        PhotoUrl = PhotoUrlFor(professional.Id, professional.PhotoUpdatedAt),
        Skills = [.. skills.Select(SkillModel.From)],
        Experiences = [.. experiences.Select(ExperienceModel.From)],
        Education = [.. education.Select(EducationModel.From)],
    };

    /// <summary>The version query string busts browser caches when the photo is replaced.</summary>
    public static string? PhotoUrlFor(Guid professionalId, DateTimeOffset? photoUpdatedAt) =>
        photoUpdatedAt is null
            ? null
            : $"/api/v1/professionals/{professionalId}/photo?v={photoUpdatedAt.Value.ToUnixTimeMilliseconds()}";
}

public class ProfessionalDetailsInput
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }

    public void ApplyTo(Professional professional)
    {
        professional.FullName = FullName.Trim();
        professional.Email = Email.Trim();
        professional.Headline = Headline?.Trim();
        professional.Location = Location?.Trim();
        professional.Summary = Summary?.Trim();
    }
}

public class PhotoModel
{
    public string ContentType { get; set; } = null!;
    public byte[] Content { get; set; } = [];
}

public class PhotoUploadModel
{
    public string PhotoUrl { get; set; } = null!;
}
