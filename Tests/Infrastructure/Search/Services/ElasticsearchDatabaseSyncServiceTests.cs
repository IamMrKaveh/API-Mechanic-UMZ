using System.Data;
using Application.Common.Contracts;
using Application.Search.Contracts;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Infrastructure.Search.Services;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Search.Services;

/// <summary>
/// Documents the current behavior of <see cref="ElasticsearchDatabaseSyncService"/>.
/// NOTE: every method below issues raw SQL with unquoted identifiers
/// (e.g. <c>FROM Categories</c>), while EF Core creates quoted PascalCase
/// tables (e.g. <c>"Categories"</c>). PostgreSQL folds unquoted names to
/// lowercase, so each call fails with 42P01 (undefined table) against the
/// real schema. These tests pin that behavior until the SQL is fixed to use
/// quoted identifiers.
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ElasticsearchDatabaseSyncServiceTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory = Substitute.For<ISqlConnectionFactory>();
    private readonly ISearchService _searchService = Substitute.For<ISearchService>();
    private readonly IElasticBulkService _bulkService = Substitute.For<IElasticBulkService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private ElasticsearchDatabaseSyncService _sut = null!;

    protected override Task OnInitializeAsync()
    {
        _sqlConnectionFactory.CreateConnectionAsync()
            .Returns(_ => (IDbConnection)new NpgsqlConnection(Fixture.ConnectionString));
        _sut = new ElasticsearchDatabaseSyncService(
            _sqlConnectionFactory, _searchService, _bulkService, _auditService);
        return Task.CompletedTask;
    }

    private static async Task<PostgresException> ShouldThrowUndefinedTableAsync(Func<Task> act)
    {
        var ex = await Should.ThrowAsync<PostgresException>(act);
        ex.SqlState.ShouldBe("42P01");
        return ex;
    }

    [Fact]
    public async Task SyncProductAsync_AgainstCurrentSchema_ThrowsUndefinedTable()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.SyncProductAsync(ProductId.NewId(), CancellationToken.None));

        await _searchService.DidNotReceiveWithAnyArgs().IndexProductAsync(default!, default);
    }

    [Fact]
    public async Task SyncCategoryAsync_AgainstCurrentSchema_ThrowsUndefinedTable()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.SyncCategoryAsync(CategoryId.NewId(), CancellationToken.None));

        await _searchService.DidNotReceiveWithAnyArgs().IndexCategoryAsync(default!, default);
    }

    [Fact]
    public async Task SyncBrandAsync_AgainstCurrentSchema_ThrowsUndefinedTable()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.SyncBrandAsync(BrandId.NewId(), CancellationToken.None));

        await _searchService.DidNotReceiveWithAnyArgs().IndexBrandAsync(default!, default);
    }

    [Fact]
    public async Task SyncAllProductsAsync_AgainstCurrentSchema_ThrowsUndefinedTable()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.SyncAllProductsAsync(CancellationToken.None));

        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexProductsAsync(default!, default);
    }

    [Fact]
    public async Task SyncAllCategoriesAsync_AgainstCurrentSchema_ThrowsUndefinedTable()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.SyncAllCategoriesAsync(CancellationToken.None));

        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexCategoriesAsync(default!, default);
    }

    [Fact]
    public async Task SyncAllBrandsAsync_AgainstCurrentSchema_ThrowsUndefinedTable()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.SyncAllBrandsAsync(CancellationToken.None));

        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexBrandsAsync(default!, default);
    }

    [Fact]
    public async Task FullSyncAsync_AgainstCurrentSchema_ThrowsOnFirstStep()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.FullSyncAsync(CancellationToken.None));

        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexCategoriesAsync(default!, default);
        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexBrandsAsync(default!, default);
        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexProductsAsync(default!, default);
    }

    [Fact]
    public async Task SyncAsync_AgainstCurrentSchema_ThrowsWithoutCompletionLog()
    {
        await ShouldThrowUndefinedTableAsync(() =>
            _sut.SyncAsync(CancellationToken.None));

        await _auditService.DidNotReceive().LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Sync Completed")),
            Arg.Any<CancellationToken>());
    }
}
