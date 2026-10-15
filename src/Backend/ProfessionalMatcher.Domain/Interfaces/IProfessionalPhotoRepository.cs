using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Domain.Interfaces;

public interface IProfessionalPhotoRepository
{
    Task<ProfessionalPhoto?> Get(Guid professionalId, CancellationToken cancellationToken = default);

    /// <summary>Inserts or replaces the professional's photo.</summary>
    Task Save(ProfessionalPhoto photo, CancellationToken cancellationToken = default);
}
