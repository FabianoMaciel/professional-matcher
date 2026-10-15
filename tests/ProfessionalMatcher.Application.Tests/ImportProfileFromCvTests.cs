using ProfessionalMatcher.Application.Professionals;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Application.Professionals.CvImport;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Exceptions;
using ProfessionalMatcher.Domain.Interfaces;
using ProfessionalMatcher.Infra.Pdf;

namespace ProfessionalMatcher.Application.Tests;

public class ImportProfileFromCvTests
{
    private const string CvText = """
        Jane Doe
        Senior Engineer
        Dublin, Ireland | jane@example.com
        SUMMARY
        Builds things.
        SKILLS
        Languages: C#, Python
        EXPERIENCE
        Senior Engineer | Acme Corp   Jan 2020 – Present
        • Built the portal in C#.
        EDUCATION
        BSc Computer Science — University of Limerick (2008 – 2012)
        """;

    private readonly IProfessionalRepository _professionals = Substitute.For<IProfessionalRepository>();
    private readonly ISkillRepository _skills = Substitute.For<ISkillRepository>();
    private readonly IExperienceRepository _experiences = Substitute.For<IExperienceRepository>();
    private readonly IEducationRepository _education = Substitute.For<IEducationRepository>();
    private readonly IPdfTextExtractor _extractor = Substitute.For<IPdfTextExtractor>();
    private readonly Professional _professional = new() { FullName = "Existing Name", Email = "keep@example.com" };

    public ImportProfileFromCvTests()
    {
        _professionals.GetById(_professional.Id, Arg.Any<CancellationToken>()).Returns(_professional);
        _skills.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _experiences.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _education.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _extractor.ExtractText(Arg.Any<Stream>()).Returns(CvText);
    }

    private ImportProfileFromCv.Handler CreateHandler() => new(
        _professionals, _skills, _experiences, _education, _extractor,
        new ProfessionalModelAssembler(_skills, _experiences, _education));

    private ImportProfileFromCv.Request Request(bool overwrite = false) =>
        new() { ProfessionalId = _professional.Id, Content = [1, 2, 3], OverwriteDetails = overwrite };

    [Fact]
    public async Task Handle_ProfessionalMissing_ReturnsNull()
    {
        _professionals.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Professional?)null);

        var result = await CreateHandler().Handle(Request(), CancellationToken.None);

        result.Should().BeNull();
        _extractor.DidNotReceive().ExtractText(Arg.Any<Stream>());
    }

    [Fact]
    public async Task Handle_AddsSkillsExperiencesAndEducation()
    {
        var result = await CreateHandler().Handle(Request(), CancellationToken.None);

        result!.Summary.SkillsAdded.Should().Be(2);
        result.Summary.ExperiencesAdded.Should().Be(1);
        result.Summary.EducationAdded.Should().Be(1);

        await _skills.Received(1).Add(
            Arg.Is<Skill>(s => s.ProfessionalId == _professional.Id && s.Name == "C#" && s.Category == "Languages" && s.YearsOfExperience >= 5),
            Arg.Any<CancellationToken>());
        await _experiences.Received(1).Add(
            Arg.Is<Experience>(e => e.Company == "Acme Corp" && e.Role == "Senior Engineer"
                && e.StartDate == new DateOnly(2020, 1, 1) && e.EndDate == null && e.Technologies.Contains("C#")),
            Arg.Any<CancellationToken>());
        await _education.Received(1).Add(
            Arg.Is<Education>(e => e.Institution == "University of Limerick" && e.Degree == "BSc Computer Science"
                && e.StartYear == 2008 && e.EndYear == 2012),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FillsOnlyBlankDetails_AndNeverTheEmail()
    {
        _professional.Headline = "My own headline";

        var result = await CreateHandler().Handle(Request(), CancellationToken.None);

        result!.Summary.UpdatedDetails.Should().BeEquivalentTo("location", "summary");
        _professional.FullName.Should().Be("Existing Name");
        _professional.Headline.Should().Be("My own headline");
        _professional.Location.Should().Be("Dublin, Ireland");
        _professional.Summary.Should().Be("Builds things.");
        _professional.Email.Should().Be("keep@example.com");
        await _professionals.Received(1).Replace(_professional, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OverwriteDetails_ReplacesFilledDetailsButNotTheEmail()
    {
        _professional.Headline = "My own headline";

        var result = await CreateHandler().Handle(Request(overwrite: true), CancellationToken.None);

        result!.Summary.UpdatedDetails.Should().BeEquivalentTo("fullName", "headline", "location", "summary");
        _professional.FullName.Should().Be("Jane Doe");
        _professional.Headline.Should().Be("Senior Engineer");
        _professional.Email.Should().Be("keep@example.com");
    }

    [Fact]
    public async Task Handle_EntriesAlreadyOnTheProfileAreSkipped()
    {
        var id = _professional.Id;
        _skills.ListByProfessionalId(id, Arg.Any<CancellationToken>())
            .Returns([new Skill { ProfessionalId = id, Name = "c#", Category = "X" }]);
        _experiences.ListByProfessionalId(id, Arg.Any<CancellationToken>())
            .Returns([new Experience { ProfessionalId = id, Company = "ACME CORP", Role = "senior engineer", StartDate = new DateOnly(2020, 1, 1) }]);
        _education.ListByProfessionalId(id, Arg.Any<CancellationToken>())
            .Returns([new Education { ProfessionalId = id, Institution = "University of Limerick", Degree = "BSc Computer Science", StartYear = 2008 }]);

        var result = await CreateHandler().Handle(Request(), CancellationToken.None);

        result!.Summary.SkillsAdded.Should().Be(1);
        result.Summary.SkillsSkipped.Should().Be(1);
        result.Summary.ExperiencesSkipped.Should().Be(1);
        result.Summary.EducationSkipped.Should().Be(1);
        await _experiences.DidNotReceive().Add(Arg.Any<Experience>(), Arg.Any<CancellationToken>());
        await _education.DidNotReceive().Add(Arg.Any<Education>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ImportingTwiceAddsNothingTheSecondTime()
    {
        var skills = new List<Skill>();
        _skills.When(s => s.Add(Arg.Any<Skill>(), Arg.Any<CancellationToken>())).Do(c => skills.Add(c.Arg<Skill>()));
        _skills.ListByProfessionalId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => skills.ToList());

        var handler = CreateHandler();
        await handler.Handle(Request(), CancellationToken.None);
        var second = await handler.Handle(Request(), CancellationToken.None);

        second!.Summary.SkillsAdded.Should().Be(0);
        second.Summary.SkillsSkipped.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NothingRecognised_Throws()
    {
        _extractor.ExtractText(Arg.Any<Stream>()).Returns("Just some random text\nwith no sections at all\n");

        var act = () => CreateHandler().Handle(Request(), CancellationToken.None);

        await act.Should().ThrowAsync<UnreadableDocumentException>().WithMessage("*No CV content was recognised*");
    }

    [Fact]
    public async Task Handle_UnreadablePdf_PropagatesTheExtractorError()
    {
        _extractor.ExtractText(Arg.Any<Stream>()).Returns(_ => throw new UnreadableDocumentException("not a pdf"));

        var act = () => CreateHandler().Handle(Request(), CancellationToken.None);

        await act.Should().ThrowAsync<UnreadableDocumentException>();
        await _professionals.DidNotReceive().Replace(Arg.Any<Professional>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LongValuesAreTruncatedToTheColumnLimits()
    {
        _extractor.ExtractText(Arg.Any<Stream>()).Returns(
            $"Jane Doe\nEXPERIENCE\nDeveloper | {new string('A', 300)} Corp   Jan 2020 – Present\n");

        await CreateHandler().Handle(Request(), CancellationToken.None);

        await _experiences.Received(1).Add(Arg.Is<Experience>(e => e.Company.Length <= 200), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(new byte[0], false)]
    [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04, 1 }, false)] // a zip, not a PDF
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 1 }, true)] // %PDF-
    public void Validator_RequiresAPdf(byte[] content, bool expected)
    {
        new ImportProfileFromCv.Validator().Validate(new ImportProfileFromCv.Request { Content = content }).IsValid
            .Should().Be(expected);
    }

    [Fact]
    public void Validator_RejectsFilesOverTheLimit()
    {
        var content = new byte[ImportProfileFromCv.MaxSizeInBytes + 1];
        "%PDF-"u8.CopyTo(content);

        new ImportProfileFromCv.Validator().Validate(new ImportProfileFromCv.Request { Content = content }).IsValid
            .Should().BeFalse();
    }

    [Fact]
    public void RealPdf_IsReadAndParsedEndToEnd()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "reference-layout.pdf"));
        var text = new PdfPigTextExtractor().ExtractText(stream);

        var cv = CvTextParser.Parse(text, new DateOnly(2026, 10, 14));

        cv.FullName.Should().Be("Jane Alice Doe");
        cv.Headline.Should().Be("Senior Software Engineer — .NET, Azure & Cloud");
        cv.Location.Should().Be("Ireland");
        cv.Experiences.Select(e => (e.Company, e.Role, e.StartDate, e.EndDate)).Should().Equal(
            ("Acme, Widgets & Co.", "Senior Software Engineer", new DateOnly(2022, 1, 1), (DateOnly?)null),
            ("Beta Payments Corporation", ".NET Developer", new DateOnly(2020, 5, 1), new DateOnly(2021, 12, 1)),
            ("Gamma For Sales Management", "Senior .NET Developer", new DateOnly(2012, 4, 1), new DateOnly(2017, 8, 1)));
        cv.Education.Select(e => (e.Institution, e.StartYear, e.EndYear)).Should().Equal(
            ("Sirius Education", 2024, 2025), ("Grupo Uniftec, Brazil", 2012, 2016));
        cv.Skills.Select(s => s.Name).Should().Contain(["C#", ".NET", ".NET Core", "SQL Server", "Azure", "Docker", "Kubernetes"]);
        cv.Skills.Single(s => s.Name == "C#").YearsOfExperience.Should().Be(7);
        cv.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ExtractingFromANonPdfThrowsUnreadableDocument()
    {
        using var stream = new MemoryStream("definitely not a pdf"u8.ToArray());

        var act = () => new PdfPigTextExtractor().ExtractText(stream);

        act.Should().Throw<UnreadableDocumentException>();
    }
}
