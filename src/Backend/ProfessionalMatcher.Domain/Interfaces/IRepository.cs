using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Domain.Interfaces;

public interface IRepository<TEntity> : IReadonlyRepository<TEntity> where TEntity : Entity
{
    Task Add(TEntity entity, CancellationToken cancellationToken = default);

    Task Replace(TEntity entity, CancellationToken cancellationToken = default);

    Task Delete(Guid id, CancellationToken cancellationToken = default);
}
