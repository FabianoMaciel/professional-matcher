namespace ProfessionalMatcher.Domain.Entities;

public class Skill : Entity
{
    public Guid ProfessionalId { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int YearsOfExperience { get; set; }

    /// <summary>1 (basic) to 5 (expert).</summary>
    public int Level { get; set; }

    public bool IsPrimary { get; set; }

    public static Skill Reconstitute(
        Guid id,
        Guid professionalId,
        string name,
        string category,
        int yearsOfExperience,
        int level,
        bool isPrimary,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) => new()
    {
        Id = id,
        ProfessionalId = professionalId,
        Name = name,
        Category = category,
        YearsOfExperience = yearsOfExperience,
        Level = level,
        IsPrimary = isPrimary,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
    };
}
