using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

internal sealed record ExperienceDraft(string Company, string Role, DateOnly Start, DateOnly? End, string Description);

/// <summary>
/// Reads a job history. Each entry is anchored on its date range ("Jan 2022 – Present"); the job title and
/// company are then found among the anchor line and the short lines around it, and everything up to the
/// next entry is the description.
/// </summary>
internal static class CvExperienceParser
{
    private const string UnknownCompany = "Unknown company";
    private const string UnknownRole = "Unknown role";
    private const int MaxDescriptionLength = 4000;

    private static readonly Regex RoleAtCompany = new(
        @"^(?<role>.+?)\s+(?:at|na|no|em|@)\s+(?<company>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PartSeparators = new(
        @"\s+[—–−]\s+|\s+-\s+|\s+@\s+", RegexOptions.Compiled);

    private sealed record Anchor(int Line, CvDateRange Range, string Remainder);

    /// <summary>Title/company found for one entry; <see cref="Pending"/> holds two parts that could not be told apart yet.</summary>
    private sealed class Entry
    {
        public required Anchor Anchor { get; init; }
        public required List<string> Back { get; init; }
        public required List<string> Extras { get; init; }
        public required List<CvLine> Body { get; init; }
        public string? Role { get; set; }
        public string? Company { get; set; }
        public (string First, string Second)? Pending { get; set; }

        /// <summary>Whether the title came before the company; null when that could not be told from the text.</summary>
        public bool? RoleFirst { get; set; }
        public HashSet<string> UsedExtras { get; } = [];
    }

    public static List<ExperienceDraft> Parse(IReadOnlyList<CvLine> lines, DateOnly today, List<string> warnings)
    {
        var anchors = FindAnchors(lines);
        if (anchors.Count == 0)
        {
            if (lines.Count > 0) warnings.Add("The experience section has no entry with a recognisable date range, so nothing was imported from it.");
            return [];
        }

        var entries = BuildEntries(lines, anchors);
        foreach (var entry in entries) Resolve(entry);

        // Where the order ("Title | Company" or "Company | Title") could not be deduced from keywords,
        // follow what the rest of the CV does.
        var titleFirstVotes = entries.Count(e => e.RoleFirst == true);
        var companyFirstVotes = entries.Count(e => e.RoleFirst == false);
        foreach (var entry in entries.Where(e => e.Pending is not null))
        {
            var (first, second) = entry.Pending!.Value;
            var titleFirst = titleFirstVotes >= companyFirstVotes;
            entry.Role = titleFirst ? first : second;
            entry.Company = titleFirst ? second : first;
        }

        return [.. entries.Select(e => ToDraft(e, today, warnings))];
    }

    private static List<Anchor> FindAnchors(IReadOnlyList<CvLine> lines)
    {
        var anchors = new List<Anchor>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.IsBullet) continue;

            var range = CvDates.FindRange(line.Text);
            if (range is null) continue;

            var remainder = CvDates.Remove(line.Text, range.Index, range.Length);
            var looksLikeHeader = CvText.WordCount(remainder) <= 14 && (range.Index == 0 || !CvText.StartsLowercase(line.Text));
            if (looksLikeHeader) anchors.Add(new Anchor(i, range, remainder));
        }

        return anchors;
    }

    private static List<Entry> BuildEntries(IReadOnlyList<CvLine> lines, List<Anchor> anchors)
    {
        // Lines just above a date line may hold the company/title ("Acme Corp / Senior Engineer / Jan 2020 – Present").
        var backCounts = anchors.Select((anchor, k) =>
        {
            var lowerBound = k == 0 ? 0 : anchors[k - 1].Line + 1;
            var count = 0;
            for (var j = anchor.Line - 1; j >= lowerBound && count < 2 && IsHeaderCandidate(lines[j]); j--) count++;
            return count;
        }).ToList();

        var entries = new List<Entry>();
        for (var k = 0; k < anchors.Count; k++)
        {
            var anchor = anchors[k];
            var regionEnd = k + 1 < anchors.Count ? anchors[k + 1].Line - backCounts[k + 1] : lines.Count;
            var body = lines.Skip(anchor.Line + 1).Take(Math.Max(0, regionEnd - anchor.Line - 1)).ToList();

            // Short plain lines straight after the date line: "Dublin, Ireland — Fraud-prevention platform".
            var extras = body.TakeWhile(l => !l.IsBullet && l.WordCount <= 14 && CvDates.FindRange(l.Text) is null)
                .Take(2).Select(l => l.Text).ToList();

            entries.Add(new Entry
            {
                Anchor = anchor,
                Back = [.. lines.Skip(anchor.Line - backCounts[k]).Take(backCounts[k]).Select(l => l.Text)],
                Extras = extras,
                Body = body,
            });
        }

        return entries;
    }

    private static bool IsHeaderCandidate(CvLine line) =>
        !line.IsBullet
        && line.WordCount <= 10
        && CvDates.FindRange(line.Text) is null
        && !CvText.EndsLikeSentence(line.Text)
        && !CvText.StartsLowercase(line.Text);

    private static void Resolve(Entry entry)
    {
        var remainder = SplitParts(entry.Anchor.Remainder);
        remainder = SplitSingleRemainder(remainder);
        var back = entry.Back.SelectMany(SplitParts).ToList();
        var extras = entry.Extras.SelectMany(SplitParts).ToList();

        var role = PickBest(remainder, CvVocabulary.RoleScore) ?? PickBest(back, CvVocabulary.RoleScore) ?? PickBest(extras, CvVocabulary.RoleScore);
        var others = (IEnumerable<string> parts) => parts.Where(p => p != role && !CvVocabulary.LooksLikeLocation(p)).ToList();
        var remainderOthers = others(remainder);
        var backOthers = others(back);
        var extraOthers = others(extras);

        string? company = null;
        if (role is not null)
        {
            company = PickBest(remainderOthers, CvVocabulary.CompanyScore);
            if (company is null && remainder.Contains(role) && remainderOthers.Count == 1) company = remainderOthers[0];
            company ??= PickBest(backOthers, CvVocabulary.CompanyScore) ?? FirstShortName(backOthers);
            company ??= PickBest(extraOthers, CvVocabulary.CompanyScore) ?? FirstShortName(extraOthers);
        }
        else if (remainderOthers.Count == 2)
        {
            entry.Pending = (remainderOthers[0], remainderOthers[1]);
        }
        else if (remainderOthers.Count == 0 && backOthers.Count == 2)
        {
            entry.Pending = (backOthers[0], backOthers[1]);
        }
        else
        {
            company = remainderOthers.FirstOrDefault() ?? backOthers.FirstOrDefault() ?? FirstShortName(extraOthers);
        }

        entry.Role = role;
        entry.Company = company;
        entry.RoleFirst = OrderOf(role, company, remainder) ?? OrderOf(role, company, back);

        foreach (var line in entry.Extras)
        {
            if (SplitParts(line).Any(p => p == role && !remainder.Contains(p) && !back.Contains(p))
                || SplitParts(line).Any(p => p == company && !remainder.Contains(p) && !back.Contains(p)))
            {
                entry.UsedExtras.Add(line);
            }
        }
    }

    private static bool? OrderOf(string? role, string? company, List<string> parts)
    {
        var roleIndex = role is null ? -1 : parts.IndexOf(role);
        var companyIndex = company is null ? -1 : parts.IndexOf(company);
        return roleIndex >= 0 && companyIndex >= 0 ? roleIndex < companyIndex : null;
    }

    /// <summary>"Senior Engineer at Acme" and "Senior Engineer, Acme Corp" written as a single piece.</summary>
    private static List<string> SplitSingleRemainder(List<string> parts)
    {
        if (parts.Count != 1) return parts;

        var single = parts[0];
        var at = RoleAtCompany.Match(single);
        if (at.Success && CvVocabulary.RoleScore(at.Groups["role"].Value) > 0)
            return [at.Groups["role"].Value.Trim(), at.Groups["company"].Value.Trim()];

        var pieces = CvText.SplitOutsideBrackets(single, ',');
        if (pieces.Count < 2) return parts;

        var roleIndex = pieces.FindIndex(p => CvVocabulary.RoleScore(p) > 0);
        if (roleIndex < 0) return parts;

        var company = string.Join(", ", pieces.Where((p, i) => i != roleIndex && !CvVocabulary.LooksLikeLocation(p)));
        return company.Length == 0 ? [pieces[roleIndex]] : [pieces[roleIndex], company];
    }

    private static List<string> SplitParts(string text) =>
    [
        .. text.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(piece => PartSeparators.Split(piece))
            .Select(p => p.Trim(' ', '|', ',', ';'))
            .Where(p => p.Length > 0),
    ];

    private static string? PickBest(IEnumerable<string> parts, Func<string, int> score) =>
        parts.Where(p => !CvVocabulary.LooksLikeLocation(p))
            .Select(p => (Part: p, Score: score(p)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Part)
            .FirstOrDefault();

    private static string? FirstShortName(IEnumerable<string> parts) =>
        parts.FirstOrDefault(p => CvText.WordCount(p) <= 7 && CvVocabulary.RoleScore(p) == 0 && !CvText.StartsLowercase(p));

    private static ExperienceDraft ToDraft(Entry entry, DateOnly today, List<string> warnings)
    {
        var range = entry.Anchor.Range;
        var start = CvDates.ToStartDate(range.Start);
        var end = range.End is { } e ? CvDates.ToEndDate(e) : (DateOnly?)null;

        if (end is { } finish && finish < start)
        {
            warnings.Add($"Experience \"{entry.Company ?? entry.Role}\": the end date is before the start date; it was set to the start date.");
            end = start;
        }

        if (entry.Company is null) warnings.Add($"Could not tell the company of the experience \"{entry.Role}\" ({start:yyyy-MM}).");
        if (entry.Role is null) warnings.Add($"Could not tell the job title of the experience at \"{entry.Company}\" ({start:yyyy-MM}).");

        return new ExperienceDraft(
            entry.Company ?? UnknownCompany,
            entry.Role ?? UnknownRole,
            start,
            end,
            BuildDescription(entry));
    }

    private static string BuildDescription(Entry entry)
    {
        var items = new List<(string Text, bool Joinable)>();

        // The first short line under the header (usually "Place — what the company does") reads as context.
        var extras = entry.Extras.Where(x => !entry.UsedExtras.Contains(x)).ToList();
        var bodySkip = entry.Extras.Count;
        if (extras.Count > 0 && entry.Extras.IndexOf(extras[0]) == 0)
            items.Add((extras[0], false));
        foreach (var extra in extras.Skip(items.Count)) items.Add((extra, true));

        foreach (var line in entry.Body.Skip(bodySkip))
        {
            if (line.IsBullet)
            {
                items.Add(("• " + line.Body, true));
            }
            else if (items.Count > 0 && items[^1].Joinable)
            {
                items[^1] = (CvText.JoinWrapped(items[^1].Text, line.Text), true);
            }
            else
            {
                items.Add((line.Text, true));
            }
        }

        var description = string.Join('\n', items.Select(i => i.Text));
        return description.Length <= MaxDescriptionLength ? description : description[..MaxDescriptionLength].TrimEnd();
    }
}
