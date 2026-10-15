using Npgsql;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Infra.Postgres.Repositories;

internal class EducationRepository(Func<NpgsqlConnection> connectionFactory)
    : PostgresRepository(connectionFactory), IEducationRepository
{
    private const string SelectColumns =
        "id, professional_id, institution, degree, start_year, end_year, created_at, updated_at";

    public Task<Education?> GetById(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingle($"SELECT {SelectColumns} FROM education WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), Map, cancellationToken);

    public async Task<IReadOnlyList<Education>> ListByProfessionalId(Guid professionalId, CancellationToken cancellationToken = default) =>
        await Query($"SELECT {SelectColumns} FROM education WHERE professional_id = @professional_id ORDER BY start_year DESC",
            cmd => cmd.Parameters.AddWithValue("professional_id", professionalId), Map, cancellationToken);

    public Task Add(Education entity, CancellationToken cancellationToken = default) =>
        Execute(
            """
            INSERT INTO education (id, professional_id, institution, degree, start_year, end_year, created_at, updated_at)
            VALUES (@id, @professional_id, @institution, @degree, @start_year, @end_year, @created_at, @updated_at)
            """, cmd => Bind(cmd, entity), cancellationToken);

    public Task Replace(Education entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        return Execute(
            """
            UPDATE education
               SET institution = @institution, degree = @degree, start_year = @start_year,
                   end_year = @end_year, updated_at = @updated_at
             WHERE id = @id
            """, cmd => Bind(cmd, entity), cancellationToken);
    }

    public Task Delete(Guid id, CancellationToken cancellationToken = default) =>
        Execute("DELETE FROM education WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), cancellationToken);

    private static void Bind(NpgsqlCommand cmd, Education e)
    {
        cmd.Parameters.AddWithValue("id", e.Id);
        cmd.Parameters.AddWithValue("professional_id", e.ProfessionalId);
        cmd.Parameters.AddWithValue("institution", e.Institution);
        cmd.Parameters.AddWithValue("degree", e.Degree);
        cmd.Parameters.AddWithValue("start_year", e.StartYear);
        cmd.Parameters.AddWithValue("end_year", (object?)e.EndYear ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created_at", e.CreatedAt);
        cmd.Parameters.AddWithValue("updated_at", e.UpdatedAt);
    }

    private static Education Map(NpgsqlDataReader reader) => Education.Reconstitute(
        id: reader.GetGuid(0),
        professionalId: reader.GetGuid(1),
        institution: reader.GetString(2),
        degree: reader.GetString(3),
        startYear: reader.GetInt32(4),
        endYear: reader.IsDBNull(5) ? null : reader.GetInt32(5),
        createdAt: reader.GetFieldValue<DateTimeOffset>(6),
        updatedAt: reader.GetFieldValue<DateTimeOffset>(7));
}
