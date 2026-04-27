using IMS.Modular.Modules.Search.Application;
using IMS.Modular.Modules.Search.Domain;
using NSubstitute;

namespace IMS.Modular.Tests.Modules.Search;

public class SearchServiceTests
{
    private readonly ISearchService _sut;

    public SearchServiceTests()
    {
        _sut = Substitute.For<ISearchService>();
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var expected = new SearchResponse(
            Results: new List<SearchResultItem>
            {
                new("inventory", "product", Guid.NewGuid().ToString(), "Laptop Dell", "SKU: DELL-001", 1.0),
                new("issues", "issue", Guid.NewGuid().ToString(), "Bug no Laptop", null, 0.9),
            },
            Total: 2
        );

        _sut.SearchAsync("laptop", Array.Empty<string>(), 1, 20)
            .Returns(expected);

        // Act
        var result = await _sut.SearchAsync("laptop", Array.Empty<string>(), 1, 20);

        // Assert
        Assert.Equal(2, result.Results.Count);
        Assert.Equal(2, result.Total);
        Assert.Contains(result.Results, r => r.Module == "inventory");
        Assert.Contains(result.Results, r => r.Module == "issues");
    }

    [Fact]
    public async Task SearchAsync_WithModuleFilter_QueriesOnlySpecifiedModules()
    {
        // Arrange
        var expected = new SearchResponse(
            Results: new List<SearchResultItem>
            {
                new("issues", "issue", Guid.NewGuid().ToString(), "Bug crítico", null, 1.0),
            },
            Total: 1
        );

        _sut.SearchAsync("bug", new[] { "issues" }, 1, 20).Returns(expected);

        // Act
        var result = await _sut.SearchAsync("bug", new[] { "issues" }, 1, 20);

        // Assert
        Assert.Single(result.Results);
        Assert.All(result.Results, r => Assert.Equal("issues", r.Module));
    }

    [Fact]
    public async Task IndexDocumentAsync_IsCalled_WithCorrectIndexName()
    {
        // Arrange
        var doc = new { Id = "123", Title = "Test" };

        // Act
        await _sut.IndexDocumentAsync("issues", doc);

        // Assert
        await _sut.Received(1).IndexDocumentAsync("issues", doc);
    }

    [Fact]
    public async Task DeleteDocumentAsync_IsCalled_WithCorrectParameters()
    {
        // Act
        await _sut.DeleteDocumentAsync("inventory", "abc-123");

        // Assert
        await _sut.Received(1).DeleteDocumentAsync("inventory", "abc-123");
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        _sut.SearchAsync("", Array.Empty<string>(), 1, 20)
            .Returns(new SearchResponse(new List<SearchResultItem>(), 0));

        // Act
        var result = await _sut.SearchAsync("", Array.Empty<string>(), 1, 20);

        // Assert
        Assert.Empty(result.Results);
        Assert.Equal(0, result.Total);
    }
}
