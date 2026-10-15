namespace ProfessionalMatcher.Domain.Entities;

public class Education : Entity
{
    public Guid ProfessionalId { get; set; }
    public string Institution { get; set; } = null!;
    public string Degree { get; set; } = null!;
    public int StartYear { get; set; }
    public int? EndYear { get; set; }

    public static Education Reconstitute(
        Guid id,
        Guid professionalId,
        string institution,
        string degree,
        int startYear,
        int? endYear,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) => new()
    {
        Id = id,
        ProfessionalId = professionalId,
        Institution = institution,
        Degree = degree,
        StartYear = startYear,
        EndYear = endYear,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
    };
}
