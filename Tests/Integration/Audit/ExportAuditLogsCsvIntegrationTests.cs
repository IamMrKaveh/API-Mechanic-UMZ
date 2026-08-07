using System.Text;
using Application.Audit.Features.Queries.ExportAuditLogs;
using Infrastructure.Audit.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Integration.Audit;

[Collection(nameof(DatabaseCollection))]
public class ExportAuditLogsCsvIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private ExportAuditLogsHandler _sut = null!;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();

        await SeedAsync();

        var queryService = new AuditQueryService(_context);
        _sut = new ExportAuditLogsHandler(queryService);
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [SkippableFact]
    public async Task Handle_CsvExportWithEntityTypeOrder_ReturnsOnlyOrderRows()
    {
        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: "Order",
            From: null,
            To: null,
            Format: "csv",
            MaxRows: 1000);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();

        var payload = result.Value;
        payload.ContentType.ShouldBe("text/csv");
        payload.FileName.ShouldEndWith(".csv");

        var csv = Encoding.UTF8.GetString(payload.FileContent);
        var lines = csv
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        lines[0].ShouldBe("Id,UserId,EventType,Action,IpAddress,EntityType,EntityId,CreatedAt");
        lines.Count.ShouldBe(1 + 3);

        foreach (var line in lines.Skip(1))
        {
            line.ShouldContain(",Order,");
        }

        csv.ShouldNotContain(",Payment,");
        csv.ShouldNotContain(",Product,");
    }

    [SkippableFact]
    public async Task Handle_CsvExportWithoutFilters_UsesDefaultMaxRowsCap()
    {
        var query = new ExportAuditLogsQuery(
            null, null, null, null, null,
            Format: "csv",
            MaxRows: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileContent.Length.ShouldBeGreaterThan(0);
    }

    [SkippableFact]
    public async Task Handle_JsonExport_ReturnsValidJsonContentType()
    {
        var query = new ExportAuditLogsQuery(
            null, null, "Order", null, null, "json", null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ContentType.ShouldBe("application/json");
        result.Value.FileName.ShouldEndWith(".json");

        var json = Encoding.UTF8.GetString(result.Value.FileContent);
        json.ShouldStartWith("[");
        json.ShouldContain("\"EntityType\": \"Order\"");
        json.ShouldNotContain("\"EntityType\": \"Payment\"");
    }

    private async Task SeedAsync()
    {
        var seed = new[]
        {
            new AuditLogBuilder().WithEventType("Order").WithAction("Created").WithEntityType("Order").Build(),
            new AuditLogBuilder().WithEventType("Order").WithAction("Paid").WithEntityType("Order").Build(),
            new AuditLogBuilder().WithEventType("Order").WithAction("Shipped").WithEntityType("Order").Build(),
            new AuditLogBuilder().WithEventType("Payment").WithAction("Captured").WithEntityType("Payment").Build(),
            new AuditLogBuilder().WithEventType("Product").WithAction("Created").WithEntityType("Product").Build(),
        };

        _context.AuditLogs.AddRange(seed);
        await _context.SaveChangesAsync();
    }
}
