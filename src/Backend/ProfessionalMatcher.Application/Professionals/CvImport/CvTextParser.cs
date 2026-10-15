using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

/// <summary>
/// Turns the plain text of a CV into structured data using fixed rules only (no AI): it finds the section
/// headings (Experience, Technical Skills, Education... in English or Portuguese), reads each section with
/// its own parser, and estimates years/level of each skill from the jobs that mention it.
/// </summary>
public static class CvTextParser
{
    private const int PrimaryYears = 6;

    private static readonly Regex PageMarker = new(
        @"^(?:page|p[aá]gina)\s+\d+(?:\s+(?:of|de)\s+\d+)?$|^\d+\s*/\s*\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <param name="today">Used as the end of "present" when estimating how long a skill has been used.</param>
    public static ParsedCv Parse(string text, DateOnly today)
    {
        var lines = text.Split('\n')
            .Select(CvLine.From)
            .Where(l => l.Text.Length > 0 && !PageMarker.IsMatch(l.Text))
            .ToList();

        var document = CvSections.Split(lines);
        var warnings = new List<string>();

        var header = CvHeaderParser.Parse(document.Header, text);
        var summary = ReadSummary(document);

        var (skillDrafts, droppedSkills) = ReadSkills(document);
        if (droppedSkills > 0)
            warnings.Add($"{droppedSkills} item(s) in the skills section were skipped because they look like sentences, not skills.");

        var experienceLines = document.Sections.Where(s => s.Kind == CvSectionKind.Experience).SelectMany(s => s.Lines).ToList();
        var experiences = CvExperienceParser.Parse(experienceLines, today, warnings);

        var educationLines = document.Sections.Where(s => s.Kind == CvSectionKind.Education).SelectMany(s => s.Lines).ToList();
        var education = CvEducationParser.Parse(educationLines, warnings);

        if (!document.Sections.Any(s => s.Kind == CvSectionKind.Experience))
            warnings.Add("No experience section was found (looked for headings such as Experience, Work History, Experiência Profissional).");
        if (!document.Sections.Any(s => s.Kind == CvSectionKind.Skills))
            warnings.Add("No skills section was found (looked for headings such as Technical Skills, Habilidades Técnicas).");
        if (!document.Sections.Any(s => s.Kind == CvSectionKind.Education))
            warnings.Add("No education section was found (looked for headings such as Education, Formação Acadêmica).");

        var skills = EstimateSkills(skillDrafts, experiences, header.Headline, today);
        var withTechnologies = experiences
            .Select(e => new ParsedExperience(
                e.Company, e.Role, e.Start, e.End, e.Description, TechnologiesIn(skillDrafts, e.Role + "\n" + e.Description)))
            .ToList();

        return new ParsedCv(
            header.FullName,
            header.Headline,
            header.Location,
            header.Email,
            summary,
            skills,
            withTechnologies,
            [.. education.Select(e => new ParsedEducation(e.Institution, e.Degree, e.StartYear, e.EndYear))],
            [.. document.Sections.Where(s => s.Kind == CvSectionKind.Ignored).Select(s => s.Title).Distinct()],
            warnings);
    }

    private static string? ReadSummary(CvDocument document)
    {
        var lines = document.Sections.Where(s => s.Kind == CvSectionKind.Summary).SelectMany(s => s.Lines).ToList();
        if (lines.Count == 0) return null;

        var summary = lines.Select(l => l.Body).Aggregate(CvText.JoinWrapped);
        return summary.Length <= 4000 ? summary : summary[..4000].TrimEnd();
    }

    private static (List<SkillDraft> Skills, int Dropped) ReadSkills(CvDocument document)
    {
        var all = new List<SkillDraft>();
        var dropped = 0;
        foreach (var section in document.Sections.Where(s => s.Kind == CvSectionKind.Skills))
        {
            var (skills, count) = CvSkillsParser.Parse(section.Lines);
            all.AddRange(skills.Where(s => !all.Any(x => x.Name.Equals(s.Name, StringComparison.OrdinalIgnoreCase))));
            dropped += count;
        }

        return (all, dropped);
    }

    // ---- skills: years and level are estimated from the jobs that mention the skill

    private static List<ParsedSkill> EstimateSkills(
        List<SkillDraft> drafts, List<ExperienceDraft> experiences, string? headline, DateOnly today)
    {
        return [.. drafts.Select(draft =>
        {
            var matchers = Matchers(draft.Name);

            var months = MonthsCovered(experiences
                .Where(e => matchers.Any(m => m.IsMatch(e.Role + "\n" + e.Description)))
                .Select(e => (From: MonthIndex(e.Start), To: MonthIndex(e.End ?? today))));
            var estimatedYears = (int)Math.Round(months / 12.0, MidpointRounding.AwayFromZero);

            var years = draft.Years ?? estimatedYears;
            var level = draft.Level ?? LevelFor(years);
            var inHeadline = headline is not null && matchers.Any(m => m.IsMatch(headline));

            // "Main skills" are the ones the CV puts in its headline or that span most of a career (6+ years).
            return new ParsedSkill(draft.Name, draft.Category, years, level, IsPrimary: years >= PrimaryYears || level >= 5 || inHeadline);
        })];
    }

    /// <summary>A skill that is listed but never shown in a job still counts as basic knowledge (level 2).</summary>
    private static int LevelFor(int years) => years switch
    {
        >= 8 => 5,
        >= 5 => 4,
        >= 2 => 3,
        _ => 2,
    };

    private static int MonthIndex(DateOnly date) => date.Year * 12 + date.Month - 1;

    /// <summary>Total months covered by the ranges, counting overlaps once.</summary>
    private static int MonthsCovered(IEnumerable<(int From, int To)> ranges)
    {
        var total = 0;
        var coveredUntil = int.MinValue;
        foreach (var (from, to) in ranges.OrderBy(r => r.From))
        {
            var start = Math.Max(from, coveredUntil + 1);
            if (to >= start) total += to - start + 1;
            coveredUntil = Math.Max(coveredUntil, to);
        }

        return total;
    }

    private static List<string> TechnologiesIn(List<SkillDraft> skills, string experienceText) =>
    [
        .. skills.Where(s => s.Name.Length >= 2 && Matchers(s.Name).Any(m => m.IsMatch(experienceText)))
            .Select(s => s.Name)
            .Take(12),
    ];

    /// <summary>The ways a skill may be written in running text: the name, its acronym, each side of "A / B", without a plural "s".</summary>
    private static List<Regex> Matchers(string name)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };

        var acronym = Regex.Match(name, @"\((?<a>[A-Za-z][A-Za-z0-9+#.]{1,9})\)");
        if (acronym.Success)
        {
            aliases.Add(acronym.Groups["a"].Value);
            aliases.Add(Regex.Replace(name, @"\s*\(.*?\)", string.Empty).Trim());
        }

        var slash = name.Split('/', StringSplitOptions.TrimEntries);
        if (slash.Length == 2 && slash[0].Contains(' ')) // "ASP.NET WebForms/MVC", but not "CI/CD"
        {
            aliases.Add(slash[0]);
            aliases.Add(slash[1]);
        }

        foreach (var alias in aliases.ToList())
        {
            if (alias.Length > 4 && alias.EndsWith('s')) aliases.Add(alias[..^1]);
        }

        return
        [
            .. aliases.Where(a => a.Length >= 2).Select(a => new Regex(
                $@"(?<![\p{{L}}\p{{N}}_#+.]){Regex.Escape(a)}(?![\p{{L}}\p{{N}}_#+])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        ];
    }
}
