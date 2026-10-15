using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Validators;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Adds a work experience to a professional.
/// </summary>
public class CreateExperience
{
    public class Handler(IProfessionalRepository professionals, IExperienceRepository experiences)
    {
        /// <returns>The created experience, or null if the professional was not found.</returns>
        public async Task<ExperienceModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            if (await professionals.GetById(request.ProfessionalId, cancellationToken) is null) return null;

            var experience = new Experience { ProfessionalId = request.ProfessionalId };
            request.Input.ApplyTo(experience);
            await experiences.Add(experience, cancellationToken);

            return ExperienceModel.From(experience);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public ExperienceInput Input { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Input).NotNull().SetValidator(new ExperienceInputValidator());
    }
}
