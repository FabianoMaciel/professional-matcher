namespace ProfessionalMatcher.Domain.Entities;

public class Professional : Entity
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Headline { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }

    /// <summary>When the profile photo was last replaced; null when there is no photo.</summary>
    public DateTimeOffset? PhotoUpdatedAt { get; protected set; }

    /// <summary>Rehydrates a Professional from a database row.</summary>
    public static Professional Reconstitute(
        Guid id,
        string fullName,
        string email,
        string? headline,
        string? location,
        string? summary,
        DateTimeOffset? photoUpdatedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) => new()
    {
        Id = id,
        FullName = fullName,
        Email = email,
        Headline = headline,
        Location = location,
        Summary = summary,
        PhotoUpdatedAt = photoUpdatedAt,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
    };
}
