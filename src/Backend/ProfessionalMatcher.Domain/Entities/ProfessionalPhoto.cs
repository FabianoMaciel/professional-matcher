namespace ProfessionalMatcher.Domain.Entities;

/// <summary>The profile picture. Kept apart from <see cref="Professional"/> so listing a profile never loads the bytes.</summary>
public class ProfessionalPhoto
{
    public Guid ProfessionalId { get; init; }
    public string ContentType { get; init; } = null!;
    public byte[] Content { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
