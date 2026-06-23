using IMS.Modular.Modules.AnomalyDetection.Domain.Entities;

namespace IMS.Modular.Modules.AnomalyDetection.Domain;

/// <summary>
/// Configuração de threshold para detecção de anomalias.
/// Pode vir de appsettings ou banco.
/// </summary>
public sealed class AnomalyThreshold
{
    /// <summary>Máximo de ajustes/perdas por usuário em uma janela de tempo antes de alertar.</summary>
    public int MaxAdjustmentsPerWindow { get; set; } = 5;

    /// <summary>Máximo de perdas por location em uma janela antes de alertar.</summary>
    public int MaxLossesPerLocationPerWindow { get; set; } = 3;

    /// <summary>Janela de tempo em minutos para contagem de frequência.</summary>
    public int WindowMinutes { get; set; } = 60;

    /// <summary>Tipos de movimento considerados suspeitos.</summary>
    public string[] SuspiciousMovementTypes { get; set; } = ["Adjustment", "Loss", "Damage"];
}

public interface IAnomalyAlertRepository
{
    Task<List<AnomalyAlert>> GetOpenAlertsAsync(CancellationToken ct = default);
    Task<List<AnomalyAlert>> GetAllAsync(int top = 100, CancellationToken ct = default);
    Task<AnomalyAlert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AnomalyAlert alert, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
