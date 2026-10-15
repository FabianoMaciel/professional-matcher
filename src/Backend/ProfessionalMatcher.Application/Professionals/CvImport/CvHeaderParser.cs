using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

/// <summary>Reads name, headline, location and e-mail from the lines above the first section.</summary>
internal static class CvHeaderParser
{
    private static readonly Regex Email = new(@"[\w.+-]+@[\w-]+(?:\.[\w-]+)+", RegexOptions.Compiled);
    private static readonly Regex Contactish = new(
        @"@|https?:|www\.|linkedin|github|gitlab|\+?\d[\d\s().-]{6,}\d", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DocumentTitle = new(
        @"^(curriculum\s+vitae|resume|résumé|cv|curr[ií]culo(\s+vitae)?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public sealed record Header(string? FullName, string? Headline, string? Location, string? Email);

    public static Header Parse(IReadOnlyList<CvLine> lines, string wholeText)
    {
        var email = Email.Match(wholeText) is { Success: true } m ? m.Value : null;

        var candidates = lines.Where(l => l.Text.Length > 0 && !DocumentTitle.IsMatch(l.Text)).Take(8).ToList();
        var nameIndex = candidates.FindIndex(l => IsName(FirstPart(l.Text)));
        if (nameIndex < 0) return new Header(null, null, FindLocation(candidates), email);

        var nameLine = candidates[nameIndex];
        var parts = nameLine.Text.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var name = CvText.TitleCaseIfShouting(parts[0]);

        // "JANE DOE | Senior Engineer": the headline can share the name line.
        string? headline = parts.Length > 1 && !Contactish.IsMatch(parts[1]) ? parts[1] : null;
        var rest = candidates.Skip(nameIndex + 1).ToList();
        if (headline is null && rest.Count > 0 && !Contactish.IsMatch(rest[0].Text) && !LooksLikeContactLine(rest[0].Text))
            headline = rest[0].Text;

        var location = FindLocation(parts.Skip(1).Select(p => new CvLine(p, p, false)).Concat(rest).ToList());
        return new Header(name, headline is { Length: > 0 } ? Truncate(headline, 200) : null, location, email);
    }

    private static string FirstPart(string text) =>
        text.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? text;

    private static bool IsName(string text)
    {
        var words = CvText.WordCount(text);
        return words is >= 2 and <= 6
            && text.Length <= 60
            && !text.Any(char.IsDigit)
            && !Contactish.IsMatch(text)
            && !Regex.IsMatch(text, @"[|,:;@]")
            && CvVocabulary.RoleScore(text) == 0;
    }

    /// <summary>A line made only of contact pieces and a place: "Ireland | mail@x.com | linkedin.com/in/x".</summary>
    private static bool LooksLikeContactLine(string text) =>
        text.Split('|').Select(p => p.Trim()).Any(p => Contactish.IsMatch(p));

    private static string? FindLocation(IReadOnlyList<CvLine> lines)
    {
        foreach (var line in lines.Take(4))
        {
            if (!LooksLikeContactLine(line.Text) && line != lines[0] && !CvVocabulary.LooksLikeLocation(line.Text)) continue;

            foreach (var segment in CvText.SplitOutsideBrackets(line.Text.Replace('•', '|').Replace('·', '|'), '|', ';'))
            {
                if (Contactish.IsMatch(segment) || segment.Count(char.IsDigit) > 4) continue;
                if (CvText.WordCount(segment) > 6 || segment.Length > 60) continue;
                if (CvVocabulary.RoleScore(segment) > 0) continue;
                if (Regex.IsMatch(segment, @"relocat|available|open to|disponivel|disponível", RegexOptions.IgnoreCase)) continue;
                return segment;
            }
        }

        return null;
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max].TrimEnd();
}
