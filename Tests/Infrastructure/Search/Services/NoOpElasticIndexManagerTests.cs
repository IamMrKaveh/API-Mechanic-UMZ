using Infrastructure.Search.Services;

namespace Tests.Infrastructure.Search.Services;

public class NoOpElasticIndexManagerTests
{
    private readonly NoOpElasticIndexManager _sut = new();

    [Fact]
    public async Task CreateProductIndexAsync_AlwaysReturnsFalse()
    {
        (await _sut.CreateProductIndexAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateCategoryIndexAsync_AlwaysReturnsFalse()
    {
        (await _sut.CreateCategoryIndexAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateBrandIndexAsync_AlwaysReturnsFalse()
    {
        (await _sut.CreateBrandIndexAsync()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("products_v1")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteIndexAsync_RegardlessOfName_ReturnsFalse(string indexName)
    {
        (await _sut.DeleteIndexAsync(indexName)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("products_v1")]
    [InlineData("missing-index")]
    [InlineData("")]
    public async Task IndexExistsAsync_RegardlessOfName_ReturnsFalse(string indexName)
    {
        (await _sut.IndexExistsAsync(indexName)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("", "")]
    public async Task ReindexAsync_RegardlessOfIndices_ReturnsFalse(string source, string destination)
    {
        (await _sut.ReindexAsync(source, destination)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateAllIndicesAsync_AlwaysReturnsFalse()
    {
        (await _sut.CreateAllIndicesAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task AllMethods_HonourCancelledTokenByReturningFalse()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        (await _sut.CreateProductIndexAsync(cts.Token)).ShouldBeFalse();
        (await _sut.CreateCategoryIndexAsync(cts.Token)).ShouldBeFalse();
        (await _sut.CreateBrandIndexAsync(cts.Token)).ShouldBeFalse();
        (await _sut.DeleteIndexAsync("products_v1", cts.Token)).ShouldBeFalse();
        (await _sut.IndexExistsAsync("products_v1", cts.Token)).ShouldBeFalse();
        (await _sut.ReindexAsync("a", "b", cts.Token)).ShouldBeFalse();
        (await _sut.CreateAllIndicesAsync(cts.Token)).ShouldBeFalse();
    }

    [Fact]
    public async Task RepeatedCalls_ReturnConsistentResults()
    {
        (await _sut.CreateProductIndexAsync()).ShouldBeFalse();
        (await _sut.CreateProductIndexAsync()).ShouldBeFalse();
        (await _sut.IndexExistsAsync("products_v1")).ShouldBeFalse();
        (await _sut.IndexExistsAsync("products_v1")).ShouldBeFalse();
    }
}
