namespace IMS.Modular.Shared.Abstractions;

/// <summary>
/// Provides access to the current request's correlation ID.
/// </summary>
public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}
