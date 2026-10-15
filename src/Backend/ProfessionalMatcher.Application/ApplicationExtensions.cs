using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProfessionalMatcher.Application.Messaging.Wolverine;
using ProfessionalMatcher.Application.Professionals;
using ProfessionalMatcher.Domain;
using ProfessionalMatcher.Infra;

namespace ProfessionalMatcher.Application;

[ExcludeFromCodeCoverage]
public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDomain();
        services.AddScoped<ProfessionalModelAssembler>();
        services.AddWolverineBus();
        services.AddInfra(configuration);
        return services;
    }
}
