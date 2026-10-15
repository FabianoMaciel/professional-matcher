using System.Diagnostics.CodeAnalysis;
using DbUp;
using Microsoft.Extensions.Configuration;

namespace ProfessionalMatcher.Infra.Postgres;

[ExcludeFromCodeCoverage]
public static class DatabaseMigrator
{
    /// <summary>
    /// Applies the pending SQL scripts from /db/up, in file-name order. Applied scripts are tracked by DbUp
    /// in the <c>schemaversions</c> table, so each one runs only once.
    /// </summary>
    public static void Migrate(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var result = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly, name => name.EndsWith(".sql"))
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException("Database migration failed.", result.Error);
    }
}
