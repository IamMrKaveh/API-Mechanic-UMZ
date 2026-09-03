using System.Data;
using Application.Common.Contracts;
using Application.Search.Contracts;
using Infrastructure.Search.Services;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Search.Services;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ElasticsearchInitialSyncServiceTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory = Substitute.For<ISqlConnectionFactory>();
    private readonly IElasticBulkService _bulkService = Substitute.For<IElasticBulkService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private ElasticsearchInitialSyncService _sut = null!;

    protected override Task OnInitializeAsync()
    {
        _sqlConnectionFactory.CreateConnectionAsync()
            .Returns(_ => (IDbConnection)new NpgsqlConnection(Fixture.ConnectionString));
        _sut = new ElasticsearchInitialSyncService(
            _sqlConnectionFactory, _bulkService, _auditService);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SyncAllDataAsync_WhenConnectionFactoryThrows_PropagatesAfterStartLog()
    {
        _sqlConnectionFactory.CreateConnectionAsync()
            .Returns(Task.FromException<IDbConnection>(new InvalidOperationException("no database")));
        _auditService.ClearReceivedCalls();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.SyncAllDataAsync(CancellationToken.None));

        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Starting initial sync")),
            Arg.Any<CancellationToken>());
        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexCategoriesAsync(default!, default);
        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexBrandsAsync(default!, default);
        await _bulkService.DidNotReceiveWithAnyArgs().BulkIndexProductsAsync(default!, default);
        await _auditService.DidNotReceive().LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Initial sync completed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAllDataAsync_AgainstPostgresSchema_ThrowsPostgresExceptionForTSqlDialect()
    {
        // The sync queries use SQL Server dialect (bit comparisons like
        // `IsActive = 1`, OFFSET/FETCH) which PostgreSQL rejects, so the
        // initial sync cannot complete against the real schema.
        await Should.ThrowAsync<Npgsql.PostgresException>(() =>
            _sut.SyncAllDataAsync(CancellationToken.None));

        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Starting initial sync")),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceive().LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Initial sync completed")),
            Arg.Any<CancellationToken>());
    }
}
