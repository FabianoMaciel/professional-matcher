using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

/// <summary>A date as written on a CV: the month is optional ("2016" vs "Mar 2016").</summary>
internal readonly record struct CvDate(int Year, int? Month);

/// <summary>A "start - end" date range found inside a line. <see cref="End"/> is null for "present".</summary>
internal sealed record CvDateRange(int Index, int Length, CvDate Start, CvDate? End);

/// <summary>Finds date ranges in English and Portuguese: "Jan 2022 – Present", "03/2019 - 11/2021", "2012 – 2016", "jan de 2020 até hoje".</summary>
internal static class CvDates
{
    private static readonly Dictionary<string, int> Months = new()
    {
        ["jan"] = 1, ["ene"] = 1, ["feb"] = 2, ["fev"] = 2, ["mar"] = 3, ["apr"] = 4, ["abr"] = 4,
        ["may"] = 5, ["mai"] = 5, ["jun"] = 6, ["jul"] = 7, ["aug"] = 8, ["ago"] = 8, ["sep"] = 9,
        ["set"] = 9, ["oct"] = 10, ["out"] = 10, ["nov"] = 11, ["dec"] = 12, ["dez"] = 12, ["dic"] = 12,
    };

    // Longest names first so "março" wins over "mar".
    private const string MonthNames =
        "janeiro|january|jan|enero|ene|fevereiro|february|feb|fev|febrero|março|marco|march|mar|marzo|" +
        "april|abril|apr|abr|maio|may|mai|mayo|junho|june|jun|junio|julho|july|jul|julio|agosto|august|" +
        "aug|ago|setembro|september|sept|sep|set|septiembre|outubro|october|oct|out|octubre|novembro|" +
        "november|nov|noviembre|dezembro|december|dec|dez|diciembre|dic";

    private const string Year = @"(?:19|20)\d{2}";

    private static string Token(string suffix) =>
        $@"(?:(?<![\p{{L}}\d])(?<mon{suffix}>{MonthNames})\b\.?[\s,./\-]*(?:de\s+|of\s+)?(?<y1{suffix}>{Year})(?!\d)" +
        $@"|(?<![\d])(?<n{suffix}>0?[1-9]|1[0-2])\s*[/.\-]\s*(?<y2{suffix}>{Year})(?!\d)" +
        $@"|(?<![\d])(?<y3{suffix}>{Year})\s*[/.\-]\s*(?<n3{suffix}>0?[1-9]|1[0-2])(?!\d)" +
        $@"|(?<![\d])(?<y4{suffix}>{Year})(?!\d))";

    private static readonly Regex Range = new(
        $@"(?<s>{Token("s")})\s*(?:[-–—−]+|\bto\b|\buntil\b|\bthrough\b|\bthru\b|\btill\b|\bat[eé]\b|\ba\b|\bà\b|\bao\b)\s*" +
        $@"(?:(?<pres>present|current(?:ly)?|now|ongoing|today|date|atual(?:mente)?|presente|hoje|(?:o\s+)?momento|actualidad|actualmente|em\s+andamento)\b" +
        $@"|(?<e>{Token("e")}))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex SingleYear = new(
        @"(?<![\d.,])(?:19|20)\d{2}(?![\d]|,\d|\.\d)", RegexOptions.Compiled);

    private static readonly Regex Leftovers = new(
        @"\(\s*\)|\[\s*\]|^[\s|,;:\-–—]+|[\s|,;:\-–—(\[]+$", RegexOptions.Compiled);

    public static CvDateRange? FindRange(string text)
    {
        var match = Range.Match(text);
        if (!match.Success) return null;

        var start = Read(match, "s");
        var end = match.Groups["pres"].Success ? null : (CvDate?)Read(match, "e");
        return new CvDateRange(match.Index, match.Length, start, end);
    }

    /// <summary>A lone year ("Graduated 2014", "(2016)"), used when an education entry has no range.</summary>
    public static int? FindSingleYear(string text, out int index, out int length)
    {
        var match = SingleYear.Match(text);
        index = match.Index;
        length = match.Length;
        return match.Success ? int.Parse(match.Value) : null;
    }

    /// <summary>The text without the matched span and without the brackets/separators it leaves behind.</summary>
    public static string Remove(string text, int index, int length)
    {
        var joined = text[..index] + " | " + text[(index + length)..];
        joined = Regex.Replace(joined, @"\(\s*\|?\s*\)|\[\s*\|?\s*\]", " ");
        joined = Regex.Replace(joined, @"(\s*\|\s*)+", " | ");
        joined = Leftovers.Replace(joined.Trim(), string.Empty).Trim();
        return CvText.Collapse(joined);
    }

    public static DateOnly ToStartDate(CvDate date) => new(date.Year, date.Month ?? 1, 1);

    public static DateOnly ToEndDate(CvDate date) => new(date.Year, date.Month ?? 12, 1);

    private static CvDate Read(Match match, string suffix)
    {
        var groups = match.Groups;
        if (groups["mon" + suffix].Success)
            return new CvDate(int.Parse(groups["y1" + suffix].Value), MonthOf(groups["mon" + suffix].Value));
        if (groups["n" + suffix].Success)
            return new CvDate(int.Parse(groups["y2" + suffix].Value), int.Parse(groups["n" + suffix].Value));
        if (groups["y3" + suffix].Success)
            return new CvDate(int.Parse(groups["y3" + suffix].Value), int.Parse(groups["n3" + suffix].Value));
        return new CvDate(int.Parse(groups["y4" + suffix].Value), null);
    }

    private static int MonthOf(string name) => Months[CvText.Fold(name)[..3]];
}
