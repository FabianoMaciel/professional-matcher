using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Domain.Interfaces;

public interface IReadonlyRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetById(Guid id, CancellationToken cancellationToken = default);
}
