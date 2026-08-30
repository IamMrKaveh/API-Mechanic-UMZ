using System.Text;
using Application.Audit.Contracts;
using Application.Audit.Features.Queries.ExportAuditLogs;
using Application.Audit.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Audit.Features.Queries.ExportAuditLogs;

public class ExportAuditLogsHandlerTests
{
    private const int DefaultCsvMaxRows = 10_000;
    private const int DefaultJsonMaxRows = 5_000;

    private readonly IAuditQueryService _queryService = Substitute.For<IAuditQueryService>();
    private readonly ExportAuditLogsHandler _sut;

    public ExportAuditLogsHandlerTests()
    {
        _sut = new ExportAuditLogsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WithCsvFormat_ReturnsSuccessWithCsvPayload()
    {
        var payload = Encoding.UTF8.GetBytes("id,action\n1,Login");
        _queryService
            .ExportToCsvAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(payload);

        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: null,
            From: null,
            To: null,
            Format: "csv",
            MaxRows: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.FileContent.ShouldBe(payload);
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileName.ShouldEndWith(".csv");
        result.Value.FileName.ShouldStartWith("audit_logs_");
    }

    [Fact]
    public async Task Handle_WithJsonFormat_ReturnsSuccessWithJsonPayload()
    {
        var payload = Encoding.UTF8.GetBytes("[{\"id\":\"1\"}]");
        _queryService
            .ExportToJsonAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(payload);

        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: null,
            From: null,
            To: null,
            Format: "json",
            MaxRows: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.FileContent.ShouldBe(payload);
        result.Value.ContentType.ShouldBe("application/json");
        result.Value.FileName.ShouldEndWith(".json");
        result.Value.FileName.ShouldStartWith("audit_logs_");
    }

    [Theory]
    [InlineData("JSON")]
    [InlineData("Json")]
    [InlineData("jSoN")]
    public async Task Handle_JsonFormatMatchIsCaseInsensitive(string format)
    {
        _queryService
            .ExportToJsonAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: null,
            From: null,
            To: null,
            Format: format,
            MaxRows: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ContentType.ShouldBe("application/json");
        result.Value.FileName.ShouldEndWith(".json");

        await _queryService.Received(1).ExportToJsonAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>());
        await _queryService.DidNotReceive().ExportToCsvAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("xml")]
    [InlineData("")]
    [InlineData("something-else")]
    public async Task Handle_NonJsonFormat_FallsBackToCsv(string format)
    {
        _queryService
            .ExportToCsvAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: null,
            From: null,
            To: null,
            Format: format,
            MaxRows: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileName.ShouldEndWith(".csv");

        await _queryService.Received(1).ExportToCsvAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>());
        await _queryService.DidNotReceive().ExportToJsonAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCsvFormatAndNullMaxRows_UsesDefaultCsvMaxRows()
    {
        AuditExportRequest? captured = null;
        _queryService
            .ExportToCsvAsync(Arg.Do<AuditExportRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: null,
            From: null,
            To: null,
            Format: "csv",
            MaxRows: null);

        await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.MaxRows.ShouldBe(DefaultCsvMaxRows);
    }

    [Fact]
    public async Task Handle_WithJsonFormatAndNullMaxRows_UsesDefaultJsonMaxRows()
    {
        AuditExportRequest? captured = null;
        _queryService
            .ExportToJsonAsync(Arg.Do<AuditExportRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: null,
            From: null,
            To: null,
            Format: "json",
            MaxRows: null);

        await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.MaxRows.ShouldBe(DefaultJsonMaxRows);
    }

    [Theory]
    [InlineData("csv", 250)]
    [InlineData("json", 42)]
    [InlineData("csv", 1)]
    public async Task Handle_WithExplicitMaxRows_UsesProvidedValue(string format, int maxRows)
    {
        AuditExportRequest? captured = null;
        _queryService
            .ExportToCsvAsync(Arg.Do<AuditExportRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());
        _queryService
            .ExportToJsonAsync(Arg.Do<AuditExportRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(
            UserId: null,
            EventType: null,
            EntityType: null,
            From: null,
            To: null,
            Format: format,
            MaxRows: maxRows);

        await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.MaxRows.ShouldBe(maxRows);
    }

    [Fact]
    public async Task Handle_PropagatesAllFiltersToExportRequest_ForCsv()
    {
        AuditExportRequest? captured = null;
        _queryService
            .ExportToCsvAsync(Arg.Do<AuditExportRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);

        var query = new ExportAuditLogsQuery(
            UserId: userId,
            EventType: "Order",
            EntityType: "Product",
            From: from,
            To: to,
            Format: "csv",
            MaxRows: 100);

        await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.UserId.ShouldBe(userId);
        captured.EventType.ShouldBe("Order");
        captured.EntityType.ShouldBe("Product");
        captured.From.ShouldBe(from);
        captured.To.ShouldBe(to);
        captured.MaxRows.ShouldBe(100);
    }

    [Fact]
    public async Task Handle_PropagatesAllFiltersToExportRequest_ForJson()
    {
        AuditExportRequest? captured = null;
        _queryService
            .ExportToJsonAsync(Arg.Do<AuditExportRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = new ExportAuditLogsQuery(
            UserId: userId,
            EventType: "Security",
            EntityType: "User",
            From: from,
            To: to,
            Format: "json",
            MaxRows: 77);

        await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.UserId.ShouldBe(userId);
        captured.EventType.ShouldBe("Security");
        captured.EntityType.ShouldBe("User");
        captured.From.ShouldBe(from);
        captured.To.ShouldBe(to);
        captured.MaxRows.ShouldBe(77);
    }

    [Fact]
    public async Task Handle_FileName_FollowsExpectedPatternForCsv()
    {
        _queryService
            .ExportToCsvAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(null, null, null, null, null, "csv", null);

        var before = DateTime.UtcNow.AddMinutes(-1);
        var result = await _sut.Handle(query, CancellationToken.None);
        var after = DateTime.UtcNow.AddMinutes(1);

        result.ShouldBeSuccess();
        var name = result.Value.FileName;
        name.ShouldStartWith("audit_logs_");
        name.ShouldEndWith(".csv");

        var timestampPart = name.Substring("audit_logs_".Length, name.Length - "audit_logs_".Length - ".csv".Length);
        var parsed = DateTime.ParseExact(
            timestampPart,
            "yyyyMMdd_HHmm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);

        parsed.ShouldBeGreaterThanOrEqualTo(new DateTime(before.Year, before.Month, before.Day, before.Hour, before.Minute, 0, DateTimeKind.Utc));
        parsed.ShouldBeLessThanOrEqualTo(new DateTime(after.Year, after.Month, after.Day, after.Hour, after.Minute, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_FileName_FollowsExpectedPatternForJson()
    {
        _queryService
            .ExportToJsonAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(null, null, null, null, null, "json", null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.FileName.ShouldStartWith("audit_logs_");
        result.Value.FileName.ShouldEndWith(".json");
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToCsvExporter()
    {
        using var cts = new CancellationTokenSource();
        _queryService
            .ExportToCsvAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(null, null, null, null, null, "csv", null);

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).ExportToCsvAsync(Arg.Any<AuditExportRequest>(), cts.Token);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToJsonExporter()
    {
        using var cts = new CancellationTokenSource();
        _queryService
            .ExportToJsonAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(null, null, null, null, null, "json", null);

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).ExportToJsonAsync(Arg.Any<AuditExportRequest>(), cts.Token);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyPayload_WhenServiceReturnsEmpty()
    {
        _queryService
            .ExportToCsvAsync(Arg.Any<AuditExportRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditLogsQuery(null, null, null, null, null, "csv", null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.FileContent.ShouldNotBeNull();
        result.Value.FileContent.Length.ShouldBe(0);
    }
}
