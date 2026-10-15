using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

/// <summary>A skill as listed on the CV, before years/level are estimated from the experiences.</summary>
internal sealed record SkillDraft(string Name, string Category, int? Years, int? Level);

/// <summary>
/// Reads a skills section written as "Category: item, item, ..." lines (the item list may wrap onto the
/// next line, which then belongs to the same category), as plain comma/pipe/bullet separated lists, or as
/// one skill per line.
/// </summary>
internal static class CvSkillsParser
{
    private const string DefaultCategory = "Other";
    private const int MaxWordsPerSkill = 6;
    private const int MaxLengthPerSkill = 60;

    private static readonly Regex CategoryPrefix = new(
        @"^(?<category>[^:|,]{2,45}):\s*(?<rest>.*)$", RegexOptions.Compiled);

    private static readonly Regex LeadingConnector = new(
        @"^(?:and|e|&|also|plus)\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Noise = new(
        @"^(?:etc\.?|and more|and others|e outros|e mais|among others)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Qualifier = new(
        @"\s*(?:[-–:]\s*)?[\(\[]?\s*(?:(?<years>\d{1,2})\+?\s*(?:years?|yrs?|anos?)|" +
        @"(?<level>beginner|basic|basico|básico|intermediate|intermediario|intermediário|advanced|avancado|avançado|expert|especialista|proficient|fluent))\s*[\)\]]?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static (List<SkillDraft> Skills, int DroppedItems) Parse(IReadOnlyList<CvLine> lines)
    {
        var skills = new List<SkillDraft>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dropped = 0;
        var category = DefaultCategory;

        foreach (var line in lines)
        {
            var text = line.Body;
            if (text.Length == 0) continue;

            var prefix = CategoryPrefix.Match(text);
            var listText = text;
            if (prefix.Success
                && CvText.WordCount(prefix.Groups["category"].Value) <= 6
                && !prefix.Groups["category"].Value.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                category = prefix.Groups["category"].Value.Trim();
                listText = prefix.Groups["rest"].Value;
            }

            foreach (var raw in CvText.SplitOutsideBrackets(listText, ',', ';', '|', '•', '·'))
            {
                var skill = Clean(raw, category);
                if (skill is null)
                {
                    dropped++;
                    continue;
                }

                foreach (var alternative in Alternatives(skill))
                {
                    if (seen.Add(alternative.Name)) skills.Add(alternative);
                }
            }
        }

        return (skills, dropped);
    }

    private static SkillDraft? Clean(string raw, string category)
    {
        // Only the trailing dot goes: a leading one belongs to names such as ".NET".
        var item = LeadingConnector.Replace(raw.Trim(), string.Empty).TrimStart(' ', '-', '–', '"', '\'').TrimEnd(' ', '.', ':', '-', '–', '"', '\'');
        if (item.Length == 0 || Noise.IsMatch(item)) return null;

        int? years = null;
        int? level = null;
        var qualifier = Qualifier.Match(item);
        if (qualifier.Success)
        {
            if (qualifier.Groups["years"].Success) years = int.Parse(qualifier.Groups["years"].Value);
            if (qualifier.Groups["level"].Success) level = LevelOf(qualifier.Groups["level"].Value);
            item = item[..qualifier.Index].TrimEnd(' ', '.', ':', '-', '–');
        }

        if (item.Length == 0 || item.Length > MaxLengthPerSkill || CvText.WordCount(item) > MaxWordsPerSkill) return null;
        return new SkillDraft(item, category.Length > 100 ? category[..100] : category, years, level);
    }

    /// <summary>".NET / .NET Core" lists two skills; "CI/CD" and "WebForms/MVC" (no spaces around the slash) stay whole.</summary>
    private static IEnumerable<SkillDraft> Alternatives(SkillDraft skill)
    {
        var parts = skill.Name.Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts.Select(p => skill with { Name = p }) : [skill];
    }

    private static int LevelOf(string word) => CvText.Fold(word) switch
    {
        "beginner" or "basic" or "basico" => 1,
        "intermediate" or "intermediario" => 3,
        "advanced" or "avancado" or "proficient" or "fluent" => 4,
        "expert" or "especialista" => 5,
        _ => 3,
    };
}
