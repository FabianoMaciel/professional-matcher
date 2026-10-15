using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Validators;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Updates a work experience of a professional.
/// </summary>
public class UpdateExperience
{
    public class Handler(IExperienceRepository experiences)
    {
        /// <returns>The updated experience, or null if it does not exist for that professional.</returns>
        public async Task<ExperienceModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            var experience = await experiences.GetById(request.ExperienceId, cancellationToken);
            if (experience is null || experience.ProfessionalId != request.ProfessionalId) return null;

            request.Input.ApplyTo(experience);
            await experiences.Replace(experience, cancellationToken);

            return ExperienceModel.From(experience);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public Guid ExperienceId { get; set; }
        public ExperienceInput Input { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Input).NotNull().SetValidator(new ExperienceInputValidator());
    }
}
