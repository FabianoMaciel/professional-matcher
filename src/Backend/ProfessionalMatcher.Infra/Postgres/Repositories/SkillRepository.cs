using Npgsql;
using ProfessionalMatcher.Domain.Entities;
using ProfessionalMatcher.Domain.Interfaces;

namespace ProfessionalMatcher.Infra.Postgres.Repositories;

internal class SkillRepository(Func<NpgsqlConnection> connectionFactory)
    : PostgresRepository(connectionFactory), ISkillRepository
{
    private const string SelectColumns =
        "id, professional_id, name, category, years_of_experience, level, is_primary, created_at, updated_at";

    public Task<Skill?> GetById(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingle($"SELECT {SelectColumns} FROM skills WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), Map, cancellationToken);

    public async Task<IReadOnlyList<Skill>> ListByProfessionalId(Guid professionalId, CancellationToken cancellationToken = default) =>
        await Query($"SELECT {SelectColumns} FROM skills WHERE professional_id = @professional_id ORDER BY name",
            cmd => cmd.Parameters.AddWithValue("professional_id", professionalId), Map, cancellationToken);

    public Task Add(Skill entity, CancellationToken cancellationToken = default) =>
        Execute(
            """
            INSERT INTO skills (id, professional_id, name, category, years_of_experience, level, is_primary, created_at, updated_at)
            VALUES (@id, @professional_id, @name, @category, @years_of_experience, @level, @is_primary, @created_at, @updated_at)
            """, cmd => Bind(cmd, entity), cancellationToken);

    public Task Replace(Skill entity, CancellationToken cancellationToken = default)
    {
        entity.Touch();
        return Execute(
            """
            UPDATE skills
               SET name = @name, category = @category, years_of_experience = @years_of_experience,
                   level = @level, is_primary = @is_primary, updated_at = @updated_at
             WHERE id = @id
            """, cmd => Bind(cmd, entity), cancellationToken);
    }

    public Task Delete(Guid id, CancellationToken cancellationToken = default) =>
        Execute("DELETE FROM skills WHERE id = @id",
            cmd => cmd.Parameters.AddWithValue("id", id), cancellationToken);

    private static void Bind(NpgsqlCommand cmd, Skill s)
    {
        cmd.Parameters.AddWithValue("id", s.Id);
        cmd.Parameters.AddWithValue("professional_id", s.ProfessionalId);
        cmd.Parameters.AddWithValue("name", s.Name);
        cmd.Parameters.AddWithValue("category", s.Category);
        cmd.Parameters.AddWithValue("years_of_experience", s.YearsOfExperience);
        cmd.Parameters.AddWithValue("level", (short)s.Level);
        cmd.Parameters.AddWithValue("is_primary", s.IsPrimary);
        cmd.Parameters.AddWithValue("created_at", s.CreatedAt);
        cmd.Parameters.AddWithValue("updated_at", s.UpdatedAt);
    }

    private static Skill Map(NpgsqlDataReader reader) => Skill.Reconstitute(
        id: reader.GetGuid(0),
        professionalId: reader.GetGuid(1),
        name: reader.GetString(2),
        category: reader.GetString(3),
        yearsOfExperience: reader.GetInt32(4),
        level: reader.GetInt16(5),
        isPrimary: reader.GetBoolean(6),
        createdAt: reader.GetFieldValue<DateTimeOffset>(7),
        updatedAt: reader.GetFieldValue<DateTimeOffset>(8));
}
