namespace ProfessionalMatcher.Application.Professionals.CvImport;

/// <summary>What was understood from the text of a CV. Nothing here has been saved yet.</summary>
public sealed record ParsedCv(
    string? FullName,
    string? Headline,
    string? Location,
    string? Email,
    string? Summary,
    IReadOnlyList<ParsedSkill> Skills,
    IReadOnlyList<ParsedExperience> Experiences,
    IReadOnlyList<ParsedEducation> Education,
    IReadOnlyList<string> IgnoredSections,
    IReadOnlyList<string> Warnings);

public sealed record ParsedSkill(string Name, string Category, int YearsOfExperience, int Level, bool IsPrimary);

public sealed record ParsedExperience(
    string Company,
    string Role,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Description,
    IReadOnlyList<string> Technologies);

public sealed record ParsedEducation(string Institution, string Degree, int StartYear, int? EndYear);
