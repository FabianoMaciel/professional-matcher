using System.Text;
using ProfessionalMatcher.Domain.Exceptions;
using ProfessionalMatcher.Domain.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ProfessionalMatcher.Infra.Pdf;

/// <summary>
/// Reads the text layer of a PDF and rebuilds its visual lines: words that share a baseline are one line,
/// ordered left to right, so a right-aligned date stays on the same line as its job title.
/// </summary>
public class PdfPigTextExtractor : IPdfTextExtractor
{
    // A horizontal gap wider than this many font-heights is a visual column break (e.g. a right-aligned date).
    private const double ColumnGapInFontHeights = 2.5;

    public string ExtractText(Stream pdf)
    {
        try
        {
            using var document = PdfDocument.Open(pdf);

            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                foreach (var line in ReadLines(page))
                    text.AppendLine(line);
            }

            if (text.ToString().Trim().Length == 0)
                throw new UnreadableDocumentException("The PDF has no text layer (is it a scanned image?).");

            return text.ToString();
        }
        catch (UnreadableDocumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new UnreadableDocumentException("The file could not be read as a PDF.", ex);
        }
    }

    private static IEnumerable<string> ReadLines(Page page)
    {
        var words = page.GetWords().Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
        if (words.Count == 0) yield break;

        var lineHeight = Median(words.Select(w => w.BoundingBox.Height));

        // Top to bottom (PDF y grows upwards); a word joins the current line while its baseline stays close.
        var lines = new List<List<Word>>();
        foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom))
        {
            var current = lines.Count > 0 ? lines[^1] : null;
            if (current is not null && Math.Abs(current[0].BoundingBox.Bottom - word.BoundingBox.Bottom) <= lineHeight * 0.5)
                current.Add(word);
            else
                lines.Add([word]);
        }

        foreach (var line in lines)
        {
            var ordered = line.OrderBy(w => w.BoundingBox.Left).ToList();
            var builder = new StringBuilder(ordered[0].Text);
            for (var i = 1; i < ordered.Count; i++)
            {
                var gap = ordered[i].BoundingBox.Left - ordered[i - 1].BoundingBox.Right;
                builder.Append(gap > lineHeight * ColumnGapInFontHeights ? "   " : " ").Append(ordered[i].Text);
            }

            yield return builder.ToString();
        }
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted[sorted.Count / 2];
    }
}
