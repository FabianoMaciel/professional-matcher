using System.Diagnostics.CodeAnalysis;
using ProfessionalMatcher.Api;
using ProfessionalMatcher.Infra.Postgres;

[assembly: ExcludeFromCodeCoverage]
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

// Opt-in (see launchSettings.json): applies the SQL scripts from /db/up before serving requests.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    DatabaseMigrator.Migrate(app.Configuration);
}

app.UseExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

// Required for WebApplicationFactory in tests
public partial class Program;
