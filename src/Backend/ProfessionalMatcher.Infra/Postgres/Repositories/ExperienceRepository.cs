using Npgsql;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Infra.Postgres.Repositories;

internal class ExperienceRepository(Func<NpgsqlConnection> connectionFactory)
    : PostgresRepository(connectionFactory), IExperienceRepository
{
    private const string SelectColumns =
        "id, professional_id, company, role, start_date, end_date, description, technologies, created_at, updated_at";

    public Task<Experience?> GetById(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingle($"SELECT {SelectColumns} FROM experiences WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), Map, cancellationToken);

    public async Task<IReadOnlyList<Experience>> ListByProfessionalId(Guid professionalId, CancellationToken cancellationToken = default) =>
        await Query($"SELECT {SelectColumns} FROM experiences WHERE professional_id = @professional_id ORDER BY start_date DESC",
            cmd => cmd.Parameters.AddWithValue("professional_id", professionalId), Map, cancellationToken);

    public Task Add(Experience entity, CancellationToken cancellationToken = default) =>
        Execute(
            """
            INSERT INTO experiences (id, professional_id, company, role, start_date, end_date, description, technologies, created_at, updated_at)
            VALUES (@id, @professional_id, @company, @role, @start_date, @end_date, @description, @technologies, @created_at, @updated_at)
            """, cmd => Bind(cmd, entity), cancellationToken);

    public Task Replace(Experience entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        return Execute(
            """
            UPDATE experiences
               SET company = @company, role = @role, start_date = @start_date, end_date = @end_date,
                   description = @description, technologies = @technologies, updated_at = @updated_at
             WHERE id = @id
            """, cmd => Bind(cmd, entity), cancellationToken);
    }

    public Task Delete(Guid id, CancellationToken cancellationToken = default) =>
        Execute("DELETE FROM experiences WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), cancellationToken);

    private static void Bind(NpgsqlCommand cmd, Experience e)
    {
        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("professional_id", e.ProfessionalId);
        cmd.Parameters.AddWithValue("company", e.Company);
        cmd.Parameters.AddWithValue("role", e.Role);
        cmd.Parameters.AddWithValue("start_date", e.StartDate);
        cmd.Parameters.AddWithValue("end_date", (object?)e.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("description", e.Description);
        cmd.Parameters.AddWithValue("technologies", e.Technologies.ToArray());
        cmd.Parameters.AddWithValue("created_at", e.CreatedAt);
        cmd.Parameters.AddWithValue("updated_at", e.UpdatedAt);
    }

    private static Experience Map(NpgsqlDataReader reader) => Experience.Reconstitute(
        id: reader.GetGuid(0),
        professionalId: reader.GetGuid(1),
        company: reader.GetString(2),
        role: reader.GetString(3),
        startDate: reader.GetFieldValue<DateOnly>(4),
        endDate: reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
        description: reader.GetString(6),
        technologies: reader.GetFieldValue<string[]>(7),
        createdAt: reader.GetFieldValue<DateTimeOffset>(8),
        updatedAt: reader.GetFieldValue<DateTimeOffset>(9));
}
