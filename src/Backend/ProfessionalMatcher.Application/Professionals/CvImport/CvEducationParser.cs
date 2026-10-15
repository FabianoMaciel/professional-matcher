namespace ProfessionalMatcher.Application.Professionals.CvImport;

internal sealed record EducationDraft(string Institution, string Degree, int StartYear, int? EndYear);

/// <summary>
/// Reads an education section. Entries are anchored on a year range ("2012 – 2016", "Aug 2024 – Dec 2025")
/// or a lone year ("Graduated 2014"). The degree and institution come from the anchor line, or from the lines
/// above/below it when the CV puts the dates on their own line.
/// </summary>
internal static class CvEducationParser
{
    private const string UnknownInstitution = "Unknown institution";
    private const string UnknownDegree = "Unknown degree";

    private enum Style
    {
        /// <summary>"Degree — Institution (2012 – 2016)": everything on one line.</summary>
        Inline,

        /// <summary>"Degree / Institution / 2012 – 2016": dates come last.</summary>
        DatesLast,

        /// <summary>"2012 – 2016 / Degree / Institution": dates come first.</summary>
        DatesFirst,
    }

    private sealed record Anchor(int Line, int StartYear, int? EndYear, string Remainder);

    public static List<EducationDraft> Parse(IReadOnlyList<CvLine> lines, List<string> warnings)
    {
        var anchors = FindAnchors(lines);
        if (anchors.Count == 0)
        {
            if (lines.Count > 0) warnings.Add("The education section has no entry with a year, so nothing was imported from it.");
            return [];
        }

        var backCounts = anchors.Select((anchor, k) =>
        {
            var lowerBound = k == 0 ? 0 : anchors[k - 1].Line + 1;
            var count = 0;
            for (var j = anchor.Line - 1; j >= lowerBound && count < 2 && IsPlainCandidate(lines[j]); j--) count++;
            return count;
        }).ToList();

        var style = DetectStyle(anchors, backCounts);

        var resolved = new List<(Anchor Anchor, string? Degree, string? Institution, (string First, string Second)? Pending, bool? DegreeFirst)>();
        for (var k = 0; k < anchors.Count; k++)
        {
            var anchor = anchors[k];
            var nextStart = k + 1 < anchors.Count ? anchors[k + 1].Line - (style == Style.DatesLast ? backCounts[k + 1] : 0) : lines.Count;
            var after = lines.Skip(anchor.Line + 1).Take(Math.Max(0, nextStart - anchor.Line - 1))
                .TakeWhile(IsPlainCandidate).Take(2).Select(l => l.Text);
            var before = lines.Skip(anchor.Line - backCounts[k]).Take(backCounts[k]).Select(l => l.Text);

            var texts = style switch
            {
                Style.DatesLast => before.Append(anchor.Remainder),
                Style.DatesFirst => after.Prepend(anchor.Remainder),
                _ => [anchor.Remainder],
            };
            var parts = texts.SelectMany(SplitParts).ToList();

            // Inline entry that only gave one side: the line right below may carry the other.
            if (style == Style.Inline && parts.Count < 2) parts.AddRange(after.SelectMany(SplitParts));

            resolved.Add(Classify(anchor, parts));
        }

        var degreeFirstVotes = resolved.Count(r => r.DegreeFirst == true);
        var institutionFirstVotes = resolved.Count(r => r.DegreeFirst == false);

        var drafts = new List<EducationDraft>();
        foreach (var (anchor, resolvedDegree, resolvedInstitution, pending, _) in resolved)
        {
            var degree = resolvedDegree;
            var institution = resolvedInstitution;
            if (pending is { } pair)
            {
                var degreeFirst = degreeFirstVotes >= institutionFirstVotes;
                degree = degreeFirst ? pair.First : pair.Second;
                institution = degreeFirst ? pair.Second : pair.First;
            }

            if (degree is null) warnings.Add($"Could not tell the degree of the education entry from {anchor.StartYear}.");
            if (institution is null) warnings.Add($"Could not tell the institution of the education entry \"{degree}\" ({anchor.StartYear}).");

            var end = anchor.EndYear is { } y && y < anchor.StartYear ? anchor.StartYear : anchor.EndYear;
            drafts.Add(new EducationDraft(institution ?? UnknownInstitution, degree ?? UnknownDegree, anchor.StartYear, end));
        }

        return drafts;
    }

    private static List<Anchor> FindAnchors(IReadOnlyList<CvLine> lines)
    {
        var anchors = new List<Anchor>();
        for (var i = 0; i < lines.Count; i++)
        {
            var text = lines[i].Body;
            if (CvText.WordCount(text) > 24) continue;

            var range = CvDates.FindRange(text);
            if (range is not null)
            {
                var end = range.End?.Year;
                anchors.Add(new Anchor(i, range.Start.Year, end, CvDates.Remove(text, range.Index, range.Length)));
                continue;
            }

            if (CvDates.FindSingleYear(text, out var index, out var length) is { } year)
                anchors.Add(new Anchor(i, year, year, CvDates.Remove(text, index, length)));
        }

        return anchors;
    }

    private static bool IsPlainCandidate(CvLine line) =>
        !line.IsBullet
        && line.WordCount <= 12
        && CvDates.FindRange(line.Text) is null
        && !CvDates.FindSingleYear(line.Text, out _, out _).HasValue
        && !CvText.EndsLikeSentence(line.Text)
        && !CvText.StartsLowercase(line.Text);

    private static Style DetectStyle(List<Anchor> anchors, List<int> backCounts)
    {
        // Anchors that carry text of their own mean everything is inline.
        if (anchors.Count(a => CvText.WordCount(a.Remainder) >= 2) * 2 >= anchors.Count) return Style.Inline;
        return backCounts[0] > 0 ? Style.DatesLast : Style.DatesFirst;
    }

    private static (Anchor, string?, string?, (string, string)?, bool?) Classify(Anchor anchor, List<string> parts)
    {
        var usable = parts.Where(p => !CvVocabulary.LooksLikeLocation(p)).ToList();

        var degree = PickBest(usable, CvVocabulary.DegreeScore);
        var institution = PickBest(usable.Where(p => p != degree), CvVocabulary.InstitutionScore);
        var left = usable.Where(p => p != degree && p != institution).ToList();

        if (degree is null && institution is null && left.Count >= 2)
            return (anchor, null, null, (left[0], left[1]), null);

        degree ??= institution is null ? left.FirstOrDefault() : left.FirstOrDefault(p => p != institution);
        institution ??= left.FirstOrDefault(p => p != degree);

        bool? degreeFirst = degree is not null && institution is not null && usable.Contains(degree) && usable.Contains(institution)
            ? usable.IndexOf(degree) < usable.IndexOf(institution)
            : null;
        return (anchor, degree, institution, null, degreeFirst);
    }

    private static string? PickBest(IEnumerable<string> parts, Func<string, int> score) =>
        parts.Select(p => (Part: p, Score: score(p))).Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).Select(x => x.Part).FirstOrDefault();

    private static IEnumerable<string> SplitParts(string text) =>
        text.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(piece => System.Text.RegularExpressions.Regex.Split(piece, @"\s+[—–−]\s+|\s+-\s+|\s+@\s+"))
            .Select(p => p.Trim(' ', '|', ',', ';'))
            .Where(p => p.Length > 0);
}
