using System.Diagnostics.CodeAnalysis;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Removes a work experience from a professional.
/// </summary>
public class DeleteExperience
{
    public class Handler(IExperienceRepository experiences)
    {
        /// <returns>True if the experience was removed; false if it does not exist for that professional.</returns>
        public async Task<bool> Handle(Request request, CancellationToken cancellationToken)
        {
            var experience = await experiences.GetById(request.ExperienceId, cancellationToken);
            if (experience is null || experience.ProfessionalId != request.ProfessionalId) return false;

            await experiences.Delete(experience.Id, cancellationToken);
            return true;
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public Guid ExperienceId { get; set; }
    }
}
