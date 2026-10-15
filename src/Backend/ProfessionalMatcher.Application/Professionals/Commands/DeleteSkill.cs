using System.Diagnostics.CodeAnalysis;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Removes a skill from a professional.
/// </summary>
public class DeleteSkill
{
    public class Handler(ISkillRepository skills)
    {
        /// <returns>True if the skill was removed; false if it does not exist for that professional.</returns>
        public async Task<bool> Handle(Request request, CancellationToken cancellationToken)
        {
            var skill = await skills.GetById(request.SkillId, cancellationToken);
            if (skill is null || skill.ProfessionalId != request.ProfessionalId) return false;

            await skills.Delete(skill.Id, cancellationToken);
            return true;
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public Guid SkillId { get; set; }
    }
}
