using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using ProfessionalMatcher.Application;

namespace ProfessionalMatcher.Api;

[ExcludeFromCodeCoverage]
public static class ApiExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddApiVersioning(o => o.DefaultApiVersion = new ApiVersion(1, 0))
                .AddMvc();
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddAuthentication();
        services.AddAuthorization();
        services.AddApplication(configuration);

        return services;
    }
}
