using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Issues.Domain.Events;
using IMS.Modular.Modules.Webhooks.Application;
using IMS.Modular.Modules.Webhooks.Application.Dispatchers;
using IMS.Modular.Modules.Webhooks.Application.DTOs;
using IMS.Modular.Modules.Webhooks.Domain;
using IMS.Modular.Modules.Webhooks.Domain.Entities;
using IMS.Modular.Modules.Webhooks.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IMS.Modular.Tests.Modules.Webhooks;

/// <summary>
/// US-069: Testes unitários para o Webhooks Module.
/// Padrão AAA (Arrange / Act / Assert).
/// </summary>
public class WebhooksTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    private static WebhooksDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<WebhooksDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new WebhooksDbContext(opts);
    }

    // ══════════════════════════════════════════════════════════════════════
    // WebhookRegistration entity
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void WebhookRegistration_NewInstance_IsActiveByDefault()
    {
        var reg = new WebhookRegistration(Guid.NewGuid(), "https://example.com/wh", "secret", [WebhookEventNames.IssueCreated]);

        Assert.True(reg.IsActive);
        Assert.Contains(WebhookEventNames.IssueCreated, reg.Events);
    }

    [Fact]
    public void WebhookRegistration_Deactivate_SetsIsActiveFalse()
    {
        var reg = new WebhookRegistration(Guid.NewGuid(), "https://example.com/wh", "secret", [WebhookEventNames.IssueCreated]);

        reg.Deactivate();

        Assert.False(reg.IsActive);
        Assert.NotNull(reg.UpdatedAt);
    }

    [Fact]
    public void WebhookRegistration_ListensTo_ReturnsTrueForRegisteredEvent()
    {
        var reg = new WebhookRegistration(Guid.NewGuid(), "https://x.com", "s", [WebhookEventNames.IssueCreated]);

        Assert.True(reg.ListensTo(WebhookEventNames.IssueCreated));
        Assert.False(reg.ListensTo(WebhookEventNames.IssueResolved));
    }

    [Fact]
    public void WebhookRegistration_ListensTo_ReturnsFalseWhenInactive()
    {
        var reg = new WebhookRegistration(Guid.NewGuid(), "https://x.com", "s", [WebhookEventNames.IssueCreated]);
        reg.Deactivate();

        Assert.False(reg.ListensTo(WebhookEventNames.IssueCreated));
    }

    // ══════════════════════════════════════════════════════════════════════
    // WebhookDelivery entity
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void WebhookDelivery_RecordAttempt_IncrementsCountAndSetsSuccess()
    {
        var delivery = new WebhookDelivery(Guid.NewGuid(), WebhookEventNames.IssueCreated, "{\"event\":\"issue.created\"}");

        delivery.RecordAttempt(true, 200, null);

        Assert.Equal(1, delivery.Attempts);
        Assert.True(delivery.Success);
        Assert.Equal(200, delivery.ResponseStatusCode);
        Assert.NotNull(delivery.LastAttemptAt);
    }

    [Fact]
    public void WebhookDelivery_RecordAttempt_MultipleAttempts_TracksFinalState()
    {
        var delivery = new WebhookDelivery(Guid.NewGuid(), WebhookEventNames.IssueCreated, "{}");

        delivery.RecordAttempt(false, 500, "Internal Server Error");
        delivery.RecordAttempt(false, 503, "Service Unavailable");
        delivery.RecordAttempt(true, 200, null);

        Assert.Equal(3, delivery.Attempts);
        Assert.True(delivery.Success);
        Assert.Equal(200, delivery.ResponseStatusCode);
    }

    // ══════════════════════════════════════════════════════════════════════
    // WebhookSigner
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void WebhookSigner_Sign_ReturnsHmacSha256Prefix()
    {
        var sig = WebhookSigner.Sign("my-secret", "{\"event\":\"test\"}");

        Assert.StartsWith("sha256=", sig);
        Assert.Equal(7 + 64, sig.Length); // "sha256=" + 64 hex chars
    }

    [Fact]
    public void WebhookSigner_Sign_SameInputProducesSameOutput()
    {
        var sig1 = WebhookSigner.Sign("secret", "payload");
        var sig2 = WebhookSigner.Sign("secret", "payload");

        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void WebhookSigner_Sign_DifferentSecretProducesDifferentOutput()
    {
        var sig1 = WebhookSigner.Sign("secret-A", "payload");
        var sig2 = WebhookSigner.Sign("secret-B", "payload");

        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void WebhookSigner_Verify_ReturnsTrueForMatchingSignature()
    {
        var payload = "{\"event\":\"issue.created\"}";
        var sig = WebhookSigner.Sign("supersecret", payload);

        Assert.True(WebhookSigner.Verify("supersecret", payload, sig));
    }

    [Fact]
    public void WebhookSigner_Verify_ReturnsFalseForTamperedPayload()
    {
        var sig = WebhookSigner.Sign("secret", "original");

        Assert.False(WebhookSigner.Verify("secret", "tampered", sig));
    }

    // ══════════════════════════════════════════════════════════════════════
    // WebhookRepository
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task WebhookRepository_AddAndGetByOwner_ReturnsRegistration()
    {
        using var db = CreateDb("repo-add-get");
        var repo = new WebhookRepository(db);
        var ownerId = Guid.NewGuid();
        var reg = new WebhookRegistration(ownerId, "https://x.com/wh", "s", [WebhookEventNames.IssueCreated]);

        await repo.AddAsync(reg, CancellationToken.None);
        var list = await repo.GetByOwnerAsync(ownerId, CancellationToken.None);

        Assert.Single(list);
        Assert.Equal(ownerId, list[0].OwnerId);
    }

    [Fact]
    public async Task WebhookRepository_GetActiveByEvent_ReturnsOnlyMatchingActive()
    {
        using var db = CreateDb("repo-active-event");
        var repo = new WebhookRepository(db);

        var active = new WebhookRegistration(Guid.NewGuid(), "https://a.com", "s1",
            [WebhookEventNames.IssueCreated]);
        var inactive = new WebhookRegistration(Guid.NewGuid(), "https://b.com", "s2",
            [WebhookEventNames.IssueCreated]);
        inactive.Deactivate();
        var otherEvent = new WebhookRegistration(Guid.NewGuid(), "https://c.com", "s3",
            [WebhookEventNames.StockLow]);

        await repo.AddAsync(active, CancellationToken.None);
        await repo.AddAsync(inactive, CancellationToken.None);
        await repo.AddAsync(otherEvent, CancellationToken.None);

        var results = await repo.GetActiveByEventAsync(WebhookEventNames.IssueCreated, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("https://a.com", results[0].Url);
    }

    // ══════════════════════════════════════════════════════════════════════
    // IssueCreatedWebhookDispatcher
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task IssueCreatedDispatcher_WithRegistration_PublishesToBus()
    {
        using var db = CreateDb("dispatcher-issue-created");
        var repo = new WebhookRepository(db);
        var reg = new WebhookRegistration(Guid.NewGuid(), "https://hook.io/cb", "sec",
            [WebhookEventNames.IssueCreated]);
        await repo.AddAsync(reg, CancellationToken.None);

        var busMock = new Mock<IMessageBus>();
        busMock.Setup(b => b.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<WebhookDeliveryMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dispatcher = new IssueCreatedWebhookDispatcher(
            repo, busMock.Object,
            NullLogger<IssueCreatedWebhookDispatcher>.Instance);

        var evt = new IssueCreatedEvent(Guid.NewGuid(), "Bug Title", IssuePriority.High, Guid.NewGuid());

        await dispatcher.Handle(evt, CancellationToken.None);

        busMock.Verify(b => b.PublishAsync(
            "ims.webhooks", "webhooks.delivery",
            It.Is<WebhookDeliveryMessage>(m =>
                m.EventName == WebhookEventNames.IssueCreated &&
                m.Url == "https://hook.io/cb" &&
                m.Signature.StartsWith("sha256=")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueCreatedDispatcher_NoRegistrations_DoesNotPublish()
    {
        using var db = CreateDb("dispatcher-no-regs");
        var repo = new WebhookRepository(db);
        var busMock = new Mock<IMessageBus>();

        var dispatcher = new IssueCreatedWebhookDispatcher(
            repo, busMock.Object,
            NullLogger<IssueCreatedWebhookDispatcher>.Instance);

        var evt = new IssueCreatedEvent(Guid.NewGuid(), "Bug", IssuePriority.Low, Guid.NewGuid());
        await dispatcher.Handle(evt, CancellationToken.None);

        busMock.Verify(b => b.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<WebhookDeliveryMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
