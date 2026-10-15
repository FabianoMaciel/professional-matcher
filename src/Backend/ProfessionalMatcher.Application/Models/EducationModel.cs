using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Application.Models;

public class EducationModel
{
    public Guid Id { get; set; }
    public string Institution { get; set; } = null!;
    public string Degree { get; set; } = null!;
    public int StartYear { get; set; }
    public int? EndYear { get; set; }

    public static EducationModel From(Education education) => new()
    {
        Id = education.Id,
        Institution = education.Institution,
        Degree = education.Degree,
        StartYear = education.StartYear,
        EndYear = education.EndYear,
    };
}

public class EducationInput
{
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public int StartYear { get; set; }
    public int? EndYear { get; set; }

    public void ApplyTo(Education education)
    {
        education.Institution = Institution.Trim();
        education.Degree = Degree.Trim();
        education.StartYear = StartYear;
        education.EndYear = EndYear;
    }
}
