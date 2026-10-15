using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ProfessionalMatcher.Domain.Interfaces;
using Wolverine;
using Wolverine.FluentValidation;

namespace ProfessionalMatcher.Application.Messaging.Wolverine;

[ExcludeFromCodeCoverage]
internal static class WolverineExtensions
{
    internal static IServiceCollection AddWolverineBus(this IServiceCollection services)
    {
        var assembly = typeof(WolverineExtensions).Assembly;

        services.AddScoped<IBusProvider, WolverineMessageProvider>();

        // In-process only for now; add a broker transport (RabbitMQ / Azure Service Bus) per environment later.
        services.AddWolverine(options =>
        {
            options.Discovery.IncludeAssembly(assembly);
            options.UseSystemTextJsonForSerialization(x =>
            {
                x.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                x.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            options.UseFluentValidation();
        });

        return services;
    }
}
