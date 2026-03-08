namespace IMS.Modular.Shared.Abstractions;

/// <summary>
/// Interface for registering Minimal API endpoint modules.
/// Each domain module implements this to register its routes.
/// </summary>
public interface IEndpointModule
{
    static abstract IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints);
}
