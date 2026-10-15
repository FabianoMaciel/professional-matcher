namespace ProfessionalMatcher.Application.Models;

public class CvImportModel
{
    /// <summary>The profile as it is after the import.</summary>
    public ProfessionalModel Profile { get; set; } = null!;

    public CvImportSummary Summary { get; set; } = new();
}

public class CvImportSummary
{
    public int SkillsAdded { get; set; }
    public int SkillsSkipped { get; set; }
    public int ExperiencesAdded { get; set; }
    public int ExperiencesSkipped { get; set; }
    public int EducationAdded { get; set; }
    public int EducationSkipped { get; set; }

    /// <summary>The general-data fields that were filled in: fullName, headline, location, summary.</summary>
    public IReadOnlyList<string> UpdatedDetails { get; set; } = [];

    /// <summary>Sections found in the CV that are not imported (certifications, languages...).</summary>
    public IReadOnlyList<string> IgnoredSections { get; set; } = [];

    /// <summary>Things the parser could not make sense of; worth a look in the profile.</summary>
    public IReadOnlyList<string> Warnings { get; set; } = [];
}
