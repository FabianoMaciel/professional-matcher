using System.Diagnostics.CodeAnalysis;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Removes an education entry from a professional.
/// </summary>
public class DeleteEducation
{
    public class Handler(IEducationRepository education)
    {
        /// <returns>True if the entry was removed; false if it does not exist for that professional.</returns>
        public async Task<bool> Handle(Request request, CancellationToken cancellationToken)
        {
            var entry = await education.GetById(request.EducationId, cancellationToken);
            if (entry is null || entry.ProfessionalId != request.ProfessionalId) return false;

            await education.Delete(entry.Id, cancellationToken);
            return true;
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public Guid EducationId { get; set; }
    }
}
