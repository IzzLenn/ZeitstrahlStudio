namespace ZeitstrahlStudio.Domain;

/// <summary>Beschreibt eine verletzte fachliche Invariante.</summary>
public sealed class DomainValidationException : ArgumentException
{
    /// <summary>Initialisiert eine fachliche Validierungsausnahme.</summary>
    public DomainValidationException(string message, string? parameterName = null)
        : base(message, parameterName)
    {
    }
}
