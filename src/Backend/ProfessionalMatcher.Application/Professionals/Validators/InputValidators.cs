using FluentValidation;
using ProfessionalMatcher.Application.Models;

namespace ProfessionalMatcher.Application.Professionals.Validators;

public class ProfessionalDetailsInputValidator : AbstractValidator<ProfessionalDetailsInput>
{
    public ProfessionalDetailsInputValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Headline).MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(4000);
    }
}

public class SkillInputValidator : AbstractValidator<SkillInput>
{
    public SkillInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 70);
        RuleFor(x => x.Level).InclusiveBetween(1, 5);
    }
}

public class ExperienceInputValidator : AbstractValidator<ExperienceInput>
{
    public ExperienceInputValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).Must(MonthFormat.IsValid).WithMessage("'Start Date' must be in the format yyyy-MM.");
        RuleFor(x => x.EndDate)
            .Must(MonthFormat.IsValid).When(x => !string.IsNullOrEmpty(x.EndDate))
            .WithMessage("'End Date' must be in the format yyyy-MM.");
        RuleFor(x => x.EndDate)
            .Must((x, end) => string.CompareOrdinal(end, x.StartDate) >= 0)
            .When(x => MonthFormat.IsValid(x.StartDate) && !string.IsNullOrEmpty(x.EndDate) && MonthFormat.IsValid(x.EndDate))
            .WithMessage("'End Date' must not be before 'Start Date'.");
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleForEach(x => x.Technologies).NotEmpty().MaximumLength(100);
    }
}

public class EducationInputValidator : AbstractValidator<EducationInput>
{
    public EducationInputValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartYear).InclusiveBetween(1900, 2100);
        RuleFor(x => x.EndYear).GreaterThanOrEqualTo(x => x.StartYear).When(x => x.EndYear.HasValue);
    }
}
