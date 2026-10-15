using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Validators;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Adds an education entry to a professional.
/// </summary>
public class CreateEducation
{
    public class Handler(IProfessionalRepository professionals, IEducationRepository education)
    {
        /// <returns>The created education, or null if the professional was not found.</returns>
        public async Task<EducationModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            if (await professionals.GetById(request.ProfessionalId, cancellationToken) is null) return null;

            var entry = new Education { ProfessionalId = request.ProfessionalId };
            request.Input.ApplyTo(entry);
            await education.Add(entry, cancellationToken);

            return EducationModel.From(entry);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public EducationInput Input { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Input).NotNull().SetValidator(new EducationInputValidator());
    }
}
