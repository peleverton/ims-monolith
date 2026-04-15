using FluentAssertions;
using IMS.Modular.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Tests.Shared.Messaging;

/// <summary>
/// US-026: Unit tests for OutboxService.
/// </summary>
public class OutboxServiceTests : IDisposable
{
    private readonly OutboxDbContext _db;
    private readonly OutboxService _sut;

    public OutboxServiceTests()
    {
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new OutboxDbContext(options);
        _sut = new OutboxService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SaveAsync_ValidMessage_PersistsToOutboxTable()
    {
        // Arrange
        var message = new { OrderId = Guid.NewGuid(), Amount = 99.99m };

        // Act
        await _sut.SaveAsync("ims.exchange", "order.created", message);

        // Assert
        var count = await _db.OutboxMessages.CountAsync();
        count.Should().Be(1);

        var saved = await _db.OutboxMessages.FirstAsync();
        saved.Exchange.Should().Be("ims.exchange");
        saved.RoutingKey.Should().Be("order.created");
        saved.Payload.Should().Contain("orderId");
        saved.ProcessedAt.Should().BeNull();
        saved.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_MultipleMessages_PersistsAll()
    {
        // Arrange & Act
        await _sut.SaveAsync("ims.exchange", "event.1", new { Id = 1 });
        await _sut.SaveAsync("ims.exchange", "event.2", new { Id = 2 });
        await _sut.SaveAsync("ims.exchange", "event.3", new { Id = 3 });

        // Assert
        var count = await _db.OutboxMessages.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task SaveAsync_SetsMessageTypeFromGenericParameter()
    {
        // Arrange
        var message = new TestEvent { Name = "test" };

        // Act
        await _sut.SaveAsync("exchange", "routing", message);

        // Assert
        var saved = await _db.OutboxMessages.FirstAsync();
        saved.MessageType.Should().Contain(nameof(TestEvent));
    }

    private sealed record TestEvent { public string Name { get; init; } = ""; }
}
