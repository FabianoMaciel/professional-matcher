using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Validators;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Adds a skill to a professional.
/// </summary>
public class CreateSkill
{
    public class Handler(IProfessionalRepository professionals, ISkillRepository skills)
    {
        /// <returns>The created skill, or null if the professional was not found.</returns>
        public async Task<SkillModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            if (await professionals.GetById(request.ProfessionalId, cancellationToken) is null) return null;

            var skill = new Skill { ProfessionalId = request.ProfessionalId };
            request.Input.ApplyTo(skill);
            await skills.Add(skill, cancellationToken);

            return SkillModel.From(skill);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public SkillInput Input { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Input).NotNull().SetValidator(new SkillInputValidator());
    }
}
