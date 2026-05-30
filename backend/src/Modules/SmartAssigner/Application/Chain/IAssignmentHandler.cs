using IMS.Modular.Modules.SmartAssigner.Domain.Models;

namespace IMS.Modular.Modules.SmartAssigner.Application.Chain;

/// <summary>
/// Base interface for a handler in the assignment Chain of Responsibility.
/// Each handler can filter candidates or short-circuit the chain.
/// </summary>
public interface IAssignmentHandler
{
    /// <summary>
    /// Process the context. Return true to continue the chain, false to stop.
    /// </summary>
    Task<bool> HandleAsync(AssignmentContext context, CancellationToken ct = default);
}
