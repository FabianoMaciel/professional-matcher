using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Application.Models;

public class ExperienceModel
{
    public Guid Id { get; set; }
    public string Company { get; set; } = null!;
    public string Role { get; set; } = null!;

    /// <summary>Format: yyyy-MM</summary>
    public string StartDate { get; set; } = null!;

    /// <summary>Format: yyyy-MM. Null when it is the current position.</summary>
    public string? EndDate { get; set; }

    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> Technologies { get; set; } = [];

    public static ExperienceModel From(Experience experience) => new()
    {
        Id = experience.Id,
        Company = experience.Company,
        Role = experience.Role,
        StartDate = MonthFormat.ToText(experience.StartDate),
        EndDate = experience.EndDate is { } end ? MonthFormat.ToText(end) : null,
        Description = experience.Description,
        Technologies = experience.Technologies,
    };
}

public class ExperienceInput
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public string? Description { get; set; }
    public List<string>? Technologies { get; set; }

    /// <remarks>Call only after validation: the dates are parsed here.</remarks>
    public void ApplyTo(Experience experience)
    {
        experience.Company = Company.Trim();
        experience.Role = Role.Trim();
        experience.StartDate = MonthFormat.Parse(StartDate);
        experience.EndDate = string.IsNullOrEmpty(EndDate) ? null : MonthFormat.Parse(EndDate);
        experience.Description = Description?.Trim() ?? string.Empty;
        experience.Technologies = [.. (Technologies ?? []).Select(t => t.Trim()).Where(t => t.Length > 0)];
    }
}
