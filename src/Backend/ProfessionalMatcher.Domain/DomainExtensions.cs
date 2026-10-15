using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Validators;

namespace ProfessionalMatcher.Domain;

[ExcludeFromCodeCoverage]
public static class DomainExtensions
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddTransient<IValidator<Professional>, ProfessionalValidator>();
        return services;
    }
}
