using Infrastructure.Search.Services;

namespace Tests.Infrastructure.Search.Services;

public class NoOpElasticsearchIndexerTests
{
    private readonly NoOpElasticsearchIndexer _sut = new();

    [Theory]
    [InlineData("Product", "Create")]
    [InlineData("Product", "Update")]
    [InlineData("Product", "Delete")]
    [InlineData("Category", "Create")]
    [InlineData("Brand", "Delete")]
    [InlineData("Unknown", "Create")]
    [InlineData("", "")]
    public async Task IndexDocumentAsync_AlwaysReturnsTrue(string entityType, string changeType)
    {
        var result = await _sut.IndexDocumentAsync(
            entityType, Guid.NewGuid(), """{"name":"x"}""", changeType);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IndexDocumentAsync_WithEmptyGuidAndEmptyDocument_ReturnsTrue()
    {
        var result = await _sut.IndexDocumentAsync("Product", Guid.Empty, string.Empty, "Create");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IndexDocumentAsync_WithCancelledToken_StillReturnsTrue()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.IndexDocumentAsync(
            "Product", Guid.NewGuid(), "{}", "Delete", cts.Token);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IndexDocumentAsync_RepeatedCalls_ReturnTrue()
    {
        var id = Guid.NewGuid();

        (await _sut.IndexDocumentAsync("Product", id, "{}", "Create")).ShouldBeTrue();
        (await _sut.IndexDocumentAsync("Product", id, "{}", "Delete")).ShouldBeTrue();
    }
}
