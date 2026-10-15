namespace ProfessionalMatcher.Domain.Exceptions;

/// <summary>Thrown when an uploaded document cannot be read (corrupt, encrypted, or without a text layer).</summary>
public class UnreadableDocumentException(string message, Exception? inner = null) : Exception(message, inner);
