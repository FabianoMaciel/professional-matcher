using ProfessionalMatcher.Domain.Entities;

namespace ProfessionalMatcher.Application.Models;

public class SkillModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int YearsOfExperience { get; set; }
    public int Level { get; set; }
    public bool IsPrimary { get; set; }

    public static SkillModel From(Skill skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name,
        Category = skill.Category,
        YearsOfExperience = skill.YearsOfExperience,
        Level = skill.Level,
        IsPrimary = skill.IsPrimary,
    };
}

public class SkillInput
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public int Level { get; set; }
    public bool IsPrimary { get; set; }

    public void ApplyTo(Skill skill)
    {
        skill.Name = Name.Trim();
        skill.Category = Category.Trim();
        skill.YearsOfExperience = YearsOfExperience;
        skill.Level = Level;
        skill.IsPrimary = IsPrimary;
    }
}
