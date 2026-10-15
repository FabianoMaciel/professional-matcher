using System.Diagnostics.CodeAnalysis;

namespace ProfessionalMatcher.Contracts.Events;

[ExcludeFromCodeCoverage]
public class ProfessionalUpdatedEvent
{
    public Guid ProfessionalId { get; set; }
    public string? Email { get; set; }
}
