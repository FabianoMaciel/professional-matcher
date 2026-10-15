using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProfessionalMatcher.Domain.Interfaces;
using ProfessionalMatcher.Infra.Pdf;
using ProfessionalMatcher.Infra.Postgres;

namespace ProfessionalMatcher.Infra;

public static class InfraExtensions
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgres(configuration);
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        return services;
    }
}
