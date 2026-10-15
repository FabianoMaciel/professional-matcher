using Npgsql;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Infra.Postgres.Repositories;

internal class ProfessionalPhotoRepository(Func<NpgsqlConnection> connectionFactory)
    : PostgresRepository(connectionFactory), IProfessionalPhotoRepository
{
    public Task<ProfessionalPhoto?> Get(Guid professionalId, CancellationToken cancellationToken = default) =>
        QuerySingle(
            "SELECT professional_id, content_type, content, updated_at FROM professional_photos WHERE professional_id = @id",
            cmd => cmd.Parameters.AddWithValue("id", professionalId),
            reader => new ProfessionalPhoto
            {
                ProfessionalId = reader.GetGuid(0),
                ContentType = reader.GetString(1),
                Content = reader.GetFieldValue<byte[]>(2),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(3),
            },
            cancellationToken);

    public Task Save(ProfessionalPhoto photo, CancellationToken cancellationToken = default) =>
        Execute(
            """
            INSERT INTO professional_photos (professional_id, content_type, content, updated_at)
            VALUES (@id, @content_type, @content, @updated_at)
            ON CONFLICT (professional_id)
            DO UPDATE SET content_type = EXCLUDED.content_type, content = EXCLUDED.content, updated_at = EXCLUDED.updated_at
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("id", photo.ProfessionalId);
                cmd.Parameters.AddWithValue("content_type", photo.ContentType);
                cmd.Parameters.AddWithValue("content", photo.Content);
                cmd.Parameters.AddWithValue("updated_at", photo.UpdatedAt);
            },
            cancellationToken);
}
