using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IMS.Modular.Tests.Integration;

/// <summary>
/// US-054: Integration tests for Issues API endpoints.
/// Covers full lifecycle: Create → Update → Status transitions → Delete.
/// Uses IntegrationWebAppFactory with isolated SQLite DBs.
/// </summary>
public class IssuesApiIntegrationTests : IClassFixture<IntegrationWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public IssuesApiIntegrationTests(IntegrationWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureAdminTokenAsync();
        if (!string.IsNullOrEmpty(_factory.AdminToken))
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/issues ───────────────────────────────────────────

    [Fact]
    public async Task GetIssues_Authenticated_Returns200()
    {
        var response = await _client.GetAsync("/api/issues");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetIssues_Unauthenticated_Returns401()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/issues");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/issues ──────────────────────────────────────────

    [Fact]
    public async Task CreateIssue_ValidPayload_Returns201WithId()
    {
        // Arrange
        var payload = new
        {
            title = "Integration test issue",
            description = "Created via integration test",
            priority = "High",
            reporterId = IntegrationSeedData.AdminUserId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/issues", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.TryGetProperty("id", out var idProp).Should().BeTrue();
        Guid.TryParse(idProp.GetString(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateIssue_EmptyTitle_Returns400()
    {
        // Arrange
        var payload = new
        {
            title = "",
            description = "Bad payload",
            priority = "Low",
            reporterId = IntegrationSeedData.AdminUserId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/issues", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/issues/{id} ──────────────────────────────────────

    [Fact]
    public async Task GetIssueById_SeededIssue_Returns200()
    {
        var response = await _client.GetAsync($"/api/issues/{IntegrationSeedData.IssueBugId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetIssueById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/issues/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Full lifecycle: Create → InProgress → Resolved ───────────

    [Fact]
    public async Task IssueLifecycle_CreateThenProgressThenResolve_ReturnsCorrectStatuses()
    {
        // Step 1: Create
        var createPayload = new
        {
            title = "Lifecycle test issue",
            description = "Testing the full lifecycle",
            priority = "Medium",
            reporterId = IntegrationSeedData.AdminUserId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/issues", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var issueId = Guid.Parse(created.GetProperty("id").GetString()!);

        // Step 2: Move to InProgress
        var inProgressResponse = await _client.PatchAsync(
            $"/api/issues/{issueId}/status",
            JsonContent.Create(new { status = "InProgress" }));
        inProgressResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Step 3: Verify status
        var getAfterProgress = await _client.GetAsync($"/api/issues/{issueId}");
        getAfterProgress.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterProgress = await getAfterProgress.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        afterProgress.GetProperty("status").GetString().Should().Be("InProgress");

        // Step 4: Resolve
        var resolvePayload = new { resolutionNotes = "Issue fully resolved in integration test" };
        var resolveResponse = await _client.PatchAsync(
            $"/api/issues/{issueId}/resolve",
            JsonContent.Create(resolvePayload));
        resolveResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Step 5: Verify resolved
        var getAfterResolve = await _client.GetAsync($"/api/issues/{issueId}");
        getAfterResolve.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterResolve = await getAfterResolve.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        afterResolve.GetProperty("status").GetString().Should().Be("Resolved");
    }

    // ── PUT /api/issues/{id} ──────────────────────────────────────

    [Fact]
    public async Task UpdateIssue_ExistingIssue_Returns200()
    {
        // Arrange
        var payload = new
        {
            title = "Updated title from integration test",
            description = "Updated description",
            priority = "Low"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/issues/{IntegrationSeedData.IssueBugId}", payload);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    // ── DELETE /api/issues/{id} ───────────────────────────────────

    [Fact]
    public async Task DeleteIssue_CreatedIssue_Returns204()
    {
        // Arrange: create a disposable issue first
        var payload = new
        {
            title = "Issue to delete",
            description = "Will be deleted",
            priority = "Low",
            reporterId = IntegrationSeedData.AdminUserId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/issues", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = Guid.Parse(body.GetProperty("id").GetString()!);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/issues/{id}");

        // Assert
        deleteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
    }
}
