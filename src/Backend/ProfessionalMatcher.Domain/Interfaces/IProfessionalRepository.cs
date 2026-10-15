using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Domain.Interfaces;

public interface IProfessionalRepository : IRepository<Professional>
{
    Task<Professional?> GetByEmail(string email, CancellationToken cancellationToken = default);
}
