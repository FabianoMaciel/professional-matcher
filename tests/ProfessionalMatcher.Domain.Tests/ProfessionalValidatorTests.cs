using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProfessionalMatcher.Domain;
using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Domain.Tests;

public class ProfessionalValidatorTests
{
    private static IValidator<Professional> CreateValidator() =>
        new ServiceCollection().AddDomain().BuildServiceProvider().GetRequiredService<IValidator<Professional>>();

    [Fact]
    public void Validate_ValidProfessional_IsValid()
    {
        var professional = new Professional { FullName = "Jane Doe", Email = "jane@example.com" };

        CreateValidator().Validate(professional).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyFullName_IsInvalid()
    {
        var professional = new Professional { FullName = "", Email = "jane@example.com" };

        CreateValidator().Validate(professional).IsValid.Should().BeFalse();
    }
}
