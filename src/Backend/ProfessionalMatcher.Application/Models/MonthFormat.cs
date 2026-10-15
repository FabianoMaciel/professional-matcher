using System.Globalization;

namespace ProfessionalMatcher.Application.Models;

/// <summary>The API exchanges month-precision dates as "yyyy-MM" strings.</summary>
public static class MonthFormat
{
    private const string Format = "yyyy-MM";

    public static bool IsValid(string? value) =>
        DateOnly.TryParseExact(
            value is null ? null : $"{value}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    public static DateOnly Parse(string value) =>
        DateOnly.ParseExact($"{value}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string ToText(DateOnly value) => value.ToString(Format, CultureInfo.InvariantCulture);
}
