using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Validators;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Application.Professionals.Commands;

/// <summary>
/// Updates an education entry of a professional.
/// </summary>
public class UpdateEducation
{
    public class Handler(IEducationRepository education)
    {
        /// <returns>The updated education, or null if it does not exist for that professional.</returns>
        public async Task<EducationModel?> Handle(Request request, CancellationToken cancellationToken)
        {
            var entry = await education.GetById(request.EducationId, cancellationToken);
            if (entry is null || entry.ProfessionalId != request.ProfessionalId) return null;

            request.Input.ApplyTo(entry);
            await education.Replace(entry, cancellationToken);

            return EducationModel.From(entry);
        }
    }

    [ExcludeFromCodeCoverage]
    public class Request
    {
        public Guid ProfessionalId { get; set; }
        public Guid EducationId { get; set; }
        public EducationInput Input { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Input).NotNull().SetValidator(new EducationInputValidator());
    }
}
