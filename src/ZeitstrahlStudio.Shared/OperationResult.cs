namespace ZeitstrahlStudio.Shared;

/// <summary>Ein lokalisierbarer, handlungsorientierter Anwendungsfehler.</summary>
public sealed record ApplicationError(string Code, string UserMessage, string? TechnicalDetails = null);

/// <summary>Explizites Ergebnis für erwartbare Fehler an Schichtgrenzen.</summary>
public sealed class OperationResult<T>
{
    private OperationResult(T? value, ApplicationError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;
    public T? Value { get; }
    public ApplicationError? Error { get; }

    /// <summary>Erzeugt ein erfolgreiches Ergebnis.</summary>
    public static OperationResult<T> Success(T value) => new(value, null);

    /// <summary>Erzeugt ein fehlgeschlagenes Ergebnis.</summary>
    public static OperationResult<T> Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new OperationResult<T>(default, error);
    }
}
