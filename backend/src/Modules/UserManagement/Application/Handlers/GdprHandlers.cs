using Hangfire;
using IMS.Modular.Modules.Jobs;
using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Modules.UserManagement.Application.DTOs;
using IMS.Modular.Modules.UserManagement.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using MediatR;

namespace IMS.Modular.Modules.UserManagement.Application.Handlers;

// ── GetDataExport ─────────────────────────────────────────────────────────────

public sealed class GetDataExportHandler(IGdprRepository repo)
    : IRequestHandler<GetDataExportQuery, DataExportDto?>
{
    public Task<DataExportDto?> Handle(GetDataExportQuery q, CancellationToken ct)
        => repo.GetDataExportAsync(q.UserId, ct);
}

// ── RequestDeletion ───────────────────────────────────────────────────────────

public sealed class RequestDeletionHandler(
    IGdprRepository repo,
    IEmailService email,
    IBackgroundJobClient jobClient,
    ILogger<RequestDeletionHandler> logger)
    : IRequestHandler<RequestDeletionCommand, DeleteRequestDto?>
{
    public async Task<DeleteRequestDto?> Handle(RequestDeletionCommand cmd, CancellationToken ct)
    {
        var request = await repo.CreateDeleteRequestAsync(cmd.UserId, ct);
        if (request is null) return null;

        // Audit log — structured event picked up by Serilog
        logger.LogWarning(
            "[AUDIT][GDPR] DeleteRequest created. UserId={UserId} RequesterId={RequesterId} " +
            "ScheduledHardDeleteAt={ScheduledAt}",
            cmd.UserId, cmd.RequesterId, request.ScheduledHardDeleteAt);

        // Schedule hard-delete job via Hangfire (runs at ScheduledHardDeleteAt)
        var delay = request.ScheduledHardDeleteAt - DateTime.UtcNow;
        jobClient.Schedule<GdprHardDeleteJob>(
            job => job.ProcessSingleAsync(cmd.UserId),
            delay > TimeSpan.Zero ? delay : TimeSpan.Zero);

        // Confirmation email (fire-and-forget, do not fail the request on email errors)
        _ = SendConfirmationEmailAsync(cmd.UserId, request.ScheduledHardDeleteAt, ct);

        return ToDto(request);
    }

    private async Task SendConfirmationEmailAsync(Guid userId, DateTime scheduledAt, CancellationToken ct)
    {
        try
        {
            // Fetch email from export (reuses same repo call path)
            var export = await repo.GetDataExportAsync(userId, ct);
            if (export is null) return;

            var subject = "Solicitação de exclusão de dados recebida";
            var body = $"""
                <p>Olá {export.User.FullName},</p>
                <p>Recebemos sua solicitação de exclusão de dados pessoais conforme a LGPD/GDPR.</p>
                <p>Sua conta foi desativada imediatamente. Todos os seus dados serão permanentemente 
                excluídos em <strong>{scheduledAt:dd/MM/yyyy}</strong>.</p>
                <p>Se você desejar cancelar esta solicitação antes dessa data, entre em contato com o administrador.</p>
                <hr/>
                <small>Este é um e-mail automático. Não responda.</small>
                """;

            await email.SendAsync(export.User.Email, subject, body, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GDPR] Failed to send deletion confirmation email for UserId={UserId}", userId);
        }
    }

    private static DeleteRequestDto ToDto(IMS.Modular.Modules.Auth.Domain.Entities.DeleteRequest r) => new(
        r.Id.ToString(),
        r.UserId.ToString(),
        r.RequestedAt,
        r.ScheduledHardDeleteAt,
        r.Status.ToString());
}

// ── CancelDeletion ────────────────────────────────────────────────────────────

public sealed class CancelDeletionHandler(
    IGdprRepository repo,
    ILogger<CancelDeletionHandler> logger)
    : IRequestHandler<CancelDeletionCommand, bool>
{
    public async Task<bool> Handle(CancelDeletionCommand cmd, CancellationToken ct)
    {
        var ok = await repo.CancelDeleteRequestAsync(cmd.UserId, cmd.AdminId, ct);

        if (ok)
        {
            logger.LogWarning(
                "[AUDIT][GDPR] DeleteRequest cancelled. UserId={UserId} AdminId={AdminId}",
                cmd.UserId, cmd.AdminId);
        }

        return ok;
    }
}
