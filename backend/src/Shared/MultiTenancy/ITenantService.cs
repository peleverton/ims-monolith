namespace IMS.Modular.Shared.MultiTenancy;

/// <summary>
/// US-078: Abstração de leitura do tenant atual para injeção em serviços e DbContexts.
/// </summary>
public interface ITenantService
{
    string? TenantId { get; }
}
