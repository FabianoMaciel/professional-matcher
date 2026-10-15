namespace ProfessionalMatcher.Domain.Exceptions;

/// <summary>Thrown when a change violates a uniqueness rule (e.g. an e-mail already in use).</summary>
public class ConflictException(string message) : Exception(message);
