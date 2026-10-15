using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Validators;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Updates a skill of a professional.
/// </summary>
public class UpdateSkill
{
    public class Handler(ISkillRepository skills)
    {
        /// <returns>The updated skill, or null if it does not exist for that professional.</returns>
        public async Task<SkillModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            var skill = await skills.GetById(request.SkillId, cancellationToken);
            if (skill is null || skill.ProfessionalId != request.ProfessionalId) return null;

            request.Input.ApplyTo(skill);
            await skills.Replace(skill, cancellationToken);

            return SkillModel.From(skill);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public Guid SkillId { get; set; }
        public SkillInput Input { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Input).NotNull().SetValidator(new SkillInputValidator());
    }
}
