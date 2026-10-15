using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

/// <summary>A line of CV text, cleaned once so every parser sees the same thing.</summary>
/// <param name="Text">Whitespace collapsed; a visual column gap (3+ spaces) becomes " | ".</param>
/// <param name="Body">Same as <paramref name="Text"/> without the leading bullet marker.</param>
internal sealed record CvLine(string Text, string Body, bool IsBullet)
{
    public static CvLine From(string raw)
    {
        var text = CvText.Normalize(raw);
        var isBullet = CvText.BulletMarker.IsMatch(text);
        var body = isBullet ? CvText.BulletMarker.Replace(text, string.Empty, 1).Trim() : text;
        return new CvLine(text, body, isBullet);
    }

    public int WordCount => CvText.WordCount(Body);
}

internal static class CvText
{
    public static readonly Regex BulletMarker = new(
        @"^\s*(?:[•●▪■◦○‣◆▶►·*]\s*|[-–—]\s+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WideGap = new(@"[ \t]{3,}|\t+", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string raw)
    {
        var text = raw
            .Replace(' ', ' ')
            .Replace("​", string.Empty)
            .Replace("﻿", string.Empty)
            .Trim();
        text = WideGap.Replace(text, " | ");
        return Spaces.Replace(text, " ").Trim();
    }

    public static string Collapse(string text) => Spaces.Replace(text, " ").Trim();

    public static int WordCount(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Lower-case, accent-free form used to compare headings and keywords.</summary>
    public static string Fold(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>The folded words of a text, split on anything that is not a letter or digit.</summary>
    public static HashSet<string> Tokens(string text) =>
        [.. Regex.Split(Fold(text), @"[^\p{L}\p{N}]+").Where(t => t.Length > 0)];

    /// <summary>Joins a wrapped line to the previous one, mending a line-end hyphen.</summary>
    public static string JoinWrapped(string previous, string next) =>
        previous.EndsWith('-') ? previous + next : previous + " " + next;

    public static bool EndsLikeSentence(string text)
    {
        var trimmed = text.TrimEnd();
        if (trimmed.EndsWith('.'))
        {
            var lastWord = trimmed.Split(' ').Last().TrimEnd('.').ToLowerInvariant();
            return !Abbreviations.Contains(lastWord);
        }

        return trimmed.EndsWith(';') || trimmed.EndsWith(',') || trimmed.EndsWith('!') || trimmed.EndsWith('?');
    }

    private static readonly HashSet<string> Abbreviations =
        ["co", "inc", "ltd", "corp", "llc", "plc", "sa", "s.a", "jr", "sr", "dr", "eng", "ltda", "cia", "univ"];

    public static bool StartsLowercase(string text) => text.Length > 0 && char.IsLetter(text[0]) && char.IsLower(text[0]);

    /// <summary>Title-cases an ALL-CAPS name, keeping the usual lower-case particles (de, da, van...).</summary>
    public static string TitleCaseIfShouting(string name)
    {
        if (name.Any(char.IsLower)) return name;

        var particles = new HashSet<string> { "de", "da", "do", "dos", "das", "e", "van", "von", "der", "di", "del", "la", "le" };
        var words = name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select((word, index) => index > 0 && particles.Contains(word) ? word : Capitalize(word));
        return string.Join(' ', words);

        static string Capitalize(string word)
        {
            var chars = word.ToCharArray();
            var upperNext = true;
            for (var i = 0; i < chars.Length; i++)
            {
                if (upperNext && char.IsLetter(chars[i])) chars[i] = char.ToUpperInvariant(chars[i]);
                upperNext = chars[i] is '-' or '\'';
            }

            return new string(chars);
        }
    }

    /// <summary>Splits on a separator that sits outside parentheses/brackets.</summary>
    public static List<string> SplitOutsideBrackets(string text, params char[] separators)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch is '(' or '[') depth++;
            else if (ch is ')' or ']') depth = Math.Max(0, depth - 1);
            else if (depth == 0 && separators.Contains(ch))
            {
                parts.Add(text[start..i]);
                start = i + 1;
            }
        }

        parts.Add(text[start..]);
        return [.. parts.Select(p => p.Trim()).Where(p => p.Length > 0)];
    }
}
