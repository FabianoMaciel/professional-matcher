using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ProfessionalMatcher.Domain.Interfaces;
using ProfessionalMatcher.Infra.Postgres.Repositories;

namespace ProfessionalMatcher.Infra.Postgres;

public static class PostgresExtension
{
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        // The app connects through PgBouncer (transaction mode): the connection string must carry
        // "No Reset On Close=true". Each operation opens and disposes its own connection.
        var connectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;

        services.AddSingleton<Func<NpgsqlConnection>>(_ => () => new NpgsqlConnection(connectionString));
        services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IExperienceRepository, ExperienceRepository>();
        services.AddScoped<IEducationRepository, EducationRepository>();
        services.AddScoped<IProfessionalPhotoRepository, ProfessionalPhotoRepository>();
        return services;
    }
}
