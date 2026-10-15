namespace ProfessionalMatcher.Domain.Interfaces;

public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts the text of a PDF as plain lines, in reading order.
    /// </summary>
    /// <exception cref="Exceptions.UnreadableDocumentException">The file is not a readable PDF, or has no text layer.</exception>
    string ExtractText(Stream pdf);
}
