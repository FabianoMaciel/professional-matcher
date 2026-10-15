using FluentValidation;
using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Domain.Validators;

internal class ProfessionalValidator : AbstractValidator<Professional>
{
    public ProfessionalValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("FullName must not be empty.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email must be a valid address.");
    }
}
