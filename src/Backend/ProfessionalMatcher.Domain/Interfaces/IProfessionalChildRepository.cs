using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Domain.Interfaces;

/// <summary>Repository for entities that belong to a single professional.</summary>
public interface IProfessionalChildRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    Task<IReadOnlyList<TEntity>> ListByProfessionalId(Guid professionalId, CancellationToken cancellationToken = default);
}
