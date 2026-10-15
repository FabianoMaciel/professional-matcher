using Npgsql;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Exceptions;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Infra.Postgres.Repositories;

internal class ProfessionalRepository(Func<NpgsqlConnection> connectionFactory)
    : PostgresRepository(connectionFactory), IProfessionalRepository
{
    // email is CITEXT; Npgsql has no built-in reader for it, so SELECTs cast to text.
    private const string SelectColumns =
        """
        id, full_name, email::text AS email, headline, location, summary,
        (SELECT updated_at FROM professional_photos WHERE professional_id = professionals.id) AS photo_updated_at,
        created_at, updated_at
        """;

    public Task<Professional?> GetById(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingle($"SELECT {SelectColumns} FROM professionals WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), Map, cancellationToken);

    public Task<Professional?> GetByEmail(string email, CancellationToken cancellationToken = default) =>
        QuerySingle($"SELECT {SelectColumns} FROM professionals WHERE email = @email",
            cmd => cmd.Parameters.AddWithValue("email", email), Map, cancellationToken);

    public Task Add(Professional entity, CancellationToken cancellationToken = default) =>
        WriteUnique(
            """
            INSERT INTO professionals (id, full_name, email, headline, location, summary, created_at, updated_at)
            VALUES (@id, @full_name, @email, @headline, @location, @summary, @created_at, @updated_at)
            """, entity, cancellationToken);

    public Task Replace(Professional entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        return WriteUnique(
            """
            UPDATE professionals
               SET full_name = @full_name, email = @email, headline = @headline,
                   location = @location, summary = @summary, updated_at = @updated_at
             WHERE id = @id
            """, entity, cancellationToken);
    }

    public Task Delete(Guid id, CancellationToken cancellationToken = default) =>
        Execute("DELETE FROM professionals WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), cancellationToken);

    private async Task WriteUnique(string sql, Professional p, CancellationToken cancellationToken)
    {
        try
        {
            await Execute(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("id", p.Id);
                cmd.Parameters.AddWithValue("full_name", p.FullName);
                cmd.Parameters.AddWithValue("email", p.Email);
                cmd.Parameters.AddWithValue("headline", (object?)p.Headline ?? DBNull.Value);
                cmd.Parameters.AddWithValue("location", (object?)p.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("summary", (object?)p.Summary ?? DBNull.Value);
                cmd.Parameters.AddWithValue("created_at", p.CreatedAt);
                cmd.Parameters.AddWithValue("updated_at", p.UpdatedAt);
            }, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException("A professional with this e-mail already exists.");
        }
    }

    private static Professional Map(NpgsqlDataReader reader) => Professional.Reconstitute(
        id: reader.GetGuid(0),
        fullName: reader.GetString(1),
        email: reader.GetString(2),
        headline: reader.IsDBNull(3) ? null : reader.GetString(3),
        location: reader.IsDBNull(4) ? null : reader.GetString(4),
        summary: reader.IsDBNull(5) ? null : reader.GetString(5),
        photoUpdatedAt: reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
        createdAt: reader.GetFieldValue<DateTimeOffset>(7),
        updatedAt: reader.GetFieldValue<DateTimeOffset>(8));
}
