namespace ProfessionalMatcher.Domain.Entities;

public class Experience : Entity
{
    public Guid ProfessionalId { get; set; }
    public string Company { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateOnly StartDate { get; set; }

    /// <summary>Null when it is the current position.</summary>
    public DateOnly? EndDate { get; set; }

    public string Description { get; set; } = string.Empty;
    public List<string> Technologies { get; set; } = [];

    public static Experience Reconstitute(
        Guid id,
        Guid professionalId,
        string company,
        string role,
        DateOnly startDate,
        DateOnly? endDate,
        string description,
        IEnumerable<string> technologies,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) => new()
    {
        Id = id,
        ProfessionalId = professionalId,
        Company = company,
        Role = role,
        StartDate = startDate,
        EndDate = endDate,
        Description = description,
        Technologies = [.. technologies],
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
    };
}
