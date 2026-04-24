using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Application.Commands;
using IMS.Modular.Modules.Notifications.Domain.Entities;
using IMS.Modular.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IMS.Modular.Tests.Modules.Notifications;

/// <summary>
/// US-066: Testes unitários para o Notifications Module.
/// </summary>
public class NotificationsTests
{
    private static NotificationsDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new NotificationsDbContext(opts);
    }

    // ── Notification entity ────────────────────────────────────────────────

    [Fact]
    public void Notification_NewInstance_IsNotRead()
    {
        var n = new Notification(Guid.NewGuid(), "IssueCreated", "New Issue", "Body text");
        Assert.False(n.IsRead);
        Assert.Null(n.ReadAt);
    }

    [Fact]
    public void Notification_MarkAsRead_SetsReadAt()
    {
        var n = new Notification(Guid.NewGuid(), "LowStock", "Low Stock Alert", "Product X is low");
        n.MarkAsRead();
        Assert.True(n.IsRead);
        Assert.NotNull(n.ReadAt);
    }

    [Fact]
    public void Notification_MarkAsRead_Twice_ReadAtNotOverwritten()
    {
        var n = new Notification(Guid.NewGuid(), "LowStock", "Alert", "Body");
        n.MarkAsRead();
        var firstReadAt = n.ReadAt;
        n.MarkAsRead();
        Assert.Equal(firstReadAt, n.ReadAt);
    }

    // ── NotificationRepository ─────────────────────────────────────────────

    [Fact]
    public async Task Repository_AddAndGet_ReturnsNotification()
    {
        var db = CreateDb($"notif-add-{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        await repo.AddAsync(new Notification(userId, "Test", "Title", "Body"));
        await repo.SaveChangesAsync();

        var items = await repo.GetByUserAsync(userId, 1, 10);
        Assert.Single(items);
        Assert.Equal("Title", items[0].Title);
    }

    [Fact]
    public async Task Repository_GetUnreadCount_OnlyCountsUnread()
    {
        var db = CreateDb($"notif-unread-{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        var read = new Notification(userId, "T", "Read", "Body");
        read.MarkAsRead();
        await repo.AddAsync(read);
        await repo.AddAsync(new Notification(userId, "T", "Unread1", "Body"));
        await repo.AddAsync(new Notification(userId, "T", "Unread2", "Body"));
        await repo.SaveChangesAsync();

        var count = await repo.GetUnreadCountAsync(userId);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Repository_GetByUser_OnlyReturnsOwnNotifications()
    {
        var db = CreateDb($"notif-own-{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        await repo.AddAsync(new Notification(userId1, "T", "For User1", "Body"));
        await repo.AddAsync(new Notification(userId2, "T", "For User2", "Body"));
        await repo.SaveChangesAsync();

        var items = await repo.GetByUserAsync(userId1, 1, 10);
        Assert.Single(items);
        Assert.Equal("For User1", items[0].Title);
    }

    [Fact]
    public async Task Repository_GetByUser_Paginates()
    {
        var db = CreateDb($"notif-page-{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
            await repo.AddAsync(new Notification(userId, "T", $"N{i}", "Body"));
        await repo.SaveChangesAsync();

        var page1 = await repo.GetByUserAsync(userId, 1, 3);
        var page2 = await repo.GetByUserAsync(userId, 2, 3);

        Assert.Equal(3, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task Repository_GetById_ReturnsCorrectNotification()
    {
        var db = CreateDb($"notif-id-{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        var n = new Notification(userId, "T", "Specific", "Body");
        await repo.AddAsync(n);
        await repo.SaveChangesAsync();

        var found = await repo.GetByIdAsync(n.Id);
        Assert.NotNull(found);
        Assert.Equal("Specific", found!.Title);
    }

    [Fact]
    public async Task Repository_GetById_ReturnsNullForUnknownId()
    {
        var db = CreateDb($"notif-null-{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var result = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ── SendNotificationHandler ────────────────────────────────────────────

    [Fact]
    public async Task SendNotificationHandler_CreatesAndPersistsNotification()
    {
        var db = CreateDb($"notif-handler-{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var handler = new SendNotificationHandler(repo);
        var userId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new SendNotificationCommand(userId, "IssueCreated", "Issue Criada", "Detalhes da issue"));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Issue Criada", result.Title);
        Assert.False(result.IsRead);

        var saved = await repo.GetByIdAsync(result.Id);
        Assert.NotNull(saved);
    }
}
