using System.Text;
using System.Text.Json;
using Application.Audit.Contracts;
using Application.Audit.Features.Shared;
using Domain.Audit.Entities;
using Domain.User.ValueObjects;
using Infrastructure.Audit.QueryServices;
using Infrastructure.Persistence.Context;

namespace Tests.Infrastructure.Audit.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AuditQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private AuditQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new AuditQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static AuditLog CreateLog(
        UserId? userId = null,
        string eventType = "UserLogin",
        string action = "Login",
        string ipAddress = "127.0.0.1",
        string? entityType = "User",
        string? entityId = null,
        string? details = "user login succeeded",
        string? userAgent = "xunit-agent")
        => AuditLog.Create(
            userId,
            eventType,
            action,
            ipAddress,
            entityType,
            entityId ?? Guid.NewGuid().ToString("N"),
            details,
            userAgent);

    [Fact]
    public async Task SearchAsync_EmptyDatabase_ReturnsEmptyListWithZeroTotal()
    {
        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest());

        logs.Count.ShouldBe(0);
        total.ShouldBe(0);
    }

    [Fact]
    public async Task SearchAsync_MultipleLogs_ReturnsAllWithTotal()
    {
        _context.AuditLogs.AddRange(
            CreateLog(),
            CreateLog(),
            CreateLog());
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest { Page = 1, PageSize = 10 });

        total.ShouldBe(3);
        logs.Count.ShouldBe(3);
    }

    [Fact]
    public async Task SearchAsync_FilteredByUserId_ReturnsMatchingOnly()
    {
        var user = UserId.NewId();
        _context.AuditLogs.Add(CreateLog(userId: user, eventType: "UserLogin"));
        _context.AuditLogs.Add(CreateLog(userId: UserId.NewId(), eventType: "UserLogin"));
        _context.AuditLogs.Add(CreateLog(userId: null, eventType: "AnonymousLogin"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            UserId = user.Value,
            Page = 1,
            PageSize = 10
        });

        total.ShouldBe(1);
        logs.Count.ShouldBe(1);
        logs[0].UserId.ShouldBe(user.Value);
    }

    [Fact]
    public async Task SearchAsync_FilteredByEventType_ReturnsMatchingOnly()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "UserLogin"));
        _context.AuditLogs.Add(CreateLog(eventType: "UserLogout"));
        _context.AuditLogs.Add(CreateLog(eventType: "OrderPlaced"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            EventType = "UserLogout",
            Page = 1,
            PageSize = 10
        });

        total.ShouldBe(1);
        logs[0].EventType.ShouldBe("UserLogout");
    }

    [Fact]
    public async Task SearchAsync_FilteredByEntityType_ReturnsMatchingOnly()
    {
        _context.AuditLogs.Add(CreateLog(entityType: "Order"));
        _context.AuditLogs.Add(CreateLog(entityType: "Product"));
        _context.AuditLogs.Add(CreateLog(entityType: "User"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            EntityType = "Order",
            Page = 1,
            PageSize = 10
        });

        total.ShouldBe(1);
        logs[0].EntityType.ShouldBe("Order");
    }

    [Fact]
    public async Task SearchAsync_FilteredByAction_ReturnsMatchingOnly()
    {
        _context.AuditLogs.Add(CreateLog(action: "Create"));
        _context.AuditLogs.Add(CreateLog(action: "Delete"));
        _context.AuditLogs.Add(CreateLog(action: "Update"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            Action = "Delete",
            Page = 1,
            PageSize = 10
        });

        total.ShouldBe(1);
        logs[0].Action.ShouldBe("Delete");
    }

    [Fact]
    public async Task SearchAsync_FilteredByKeywordInDetails_ReturnsMatching()
    {
        _context.AuditLogs.Add(CreateLog(details: "user reset password successfully"));
        _context.AuditLogs.Add(CreateLog(details: "user updated profile"));
        _context.AuditLogs.Add(CreateLog(details: "unrelated activity"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            Keyword = "password",
            Page = 1,
            PageSize = 10
        });

        total.ShouldBe(1);
        logs[0].Details!.ShouldContain("password");
    }

    [Fact]
    public async Task SearchAsync_FilteredByIpAddress_ReturnsMatchingOnly()
    {
        _context.AuditLogs.Add(CreateLog(ipAddress: "10.0.0.1"));
        _context.AuditLogs.Add(CreateLog(ipAddress: "10.0.0.2"));
        _context.AuditLogs.Add(CreateLog(ipAddress: "10.0.0.3"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            IpAddress = "10.0.0.2",
            Page = 1,
            PageSize = 10
        });

        total.ShouldBe(1);
        logs[0].IpAddress.ShouldBe("10.0.0.2");
    }

    [Fact]
    public async Task SearchAsync_SortByEventTypeDesc_ReturnsAlphabeticallyDescending()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "Bravo"));
        _context.AuditLogs.Add(CreateLog(eventType: "Alpha"));
        _context.AuditLogs.Add(CreateLog(eventType: "Charlie"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, _) = await _sut.SearchAsync(new AuditSearchRequest
        {
            SortBy = "eventtype",
            SortDesc = true,
            Page = 1,
            PageSize = 10
        });

        logs.Select(l => l.EventType).ToList().ShouldBe(new List<string> { "Charlie", "Bravo", "Alpha" });
    }

    [Fact]
    public async Task SearchAsync_SortByActionAsc_ReturnsAlphabeticallyAscending()
    {
        _context.AuditLogs.Add(CreateLog(action: "Zeta"));
        _context.AuditLogs.Add(CreateLog(action: "Alpha"));
        _context.AuditLogs.Add(CreateLog(action: "Mike"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, _) = await _sut.SearchAsync(new AuditSearchRequest
        {
            SortBy = "action",
            SortDesc = false,
            Page = 1,
            PageSize = 10
        });

        logs.Select(l => l.Action).ToList().ShouldBe(new List<string> { "Alpha", "Mike", "Zeta" });
    }

    [Fact]
    public async Task SearchAsync_DefaultSort_OrdersByCreatedAtDescending()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "First"));
        await _context.SaveChangesAsync();
        await Task.Delay(30);
        _context.AuditLogs.Add(CreateLog(eventType: "Second"));
        await _context.SaveChangesAsync();
        await Task.Delay(30);
        _context.AuditLogs.Add(CreateLog(eventType: "Third"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, _) = await _sut.SearchAsync(new AuditSearchRequest
        {
            SortDesc = true,
            Page = 1,
            PageSize = 10
        });

        logs.Count.ShouldBe(3);
        logs[0].CreatedAt.ShouldBeGreaterThanOrEqualTo(logs[1].CreatedAt);
        logs[1].CreatedAt.ShouldBeGreaterThanOrEqualTo(logs[2].CreatedAt);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ReturnsSecondPageWithSubsetAndFullTotal()
    {
        for (var i = 0; i < 5; i++)
        {
            _context.AuditLogs.Add(CreateLog(eventType: $"Evt{i}"));
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            Page = 2,
            PageSize = 2
        });

        total.ShouldBe(5);
        logs.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyDatabase_ReturnsOnlyHeader()
    {
        var bytes = await _sut.ExportToCsvAsync(new AuditExportRequest { MaxRows = 100 });

        var csv = Encoding.UTF8.GetString(bytes);
        csv.ShouldContain("Id,UserId,EventType,Action,IpAddress,EntityType,EntityId,CreatedAt");
        var lines = csv.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(1);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithLogs_ReturnsHeaderPlusRows()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "Login"));
        _context.AuditLogs.Add(CreateLog(eventType: "Logout"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bytes = await _sut.ExportToCsvAsync(new AuditExportRequest { MaxRows = 100 });

        var csv = Encoding.UTF8.GetString(bytes);
        var lines = csv.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(3);
        csv.ShouldContain("Login");
        csv.ShouldContain("Logout");
    }

    [Fact]
    public async Task ExportToCsvAsync_MaxRowsCapsResult()
    {
        for (var i = 0; i < 5; i++)
        {
            _context.AuditLogs.Add(CreateLog(eventType: $"Ev{i}"));
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bytes = await _sut.ExportToCsvAsync(new AuditExportRequest { MaxRows = 2 });

        var csv = Encoding.UTF8.GetString(bytes);
        var lines = csv.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(3);
    }

    [Fact]
    public async Task ExportToCsvAsync_ValuesWithCommas_ArePropertyEscaped()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "Login,With,Comma", action: "OK"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bytes = await _sut.ExportToCsvAsync(new AuditExportRequest { MaxRows = 10 });

        var csv = Encoding.UTF8.GetString(bytes);
        csv.ShouldContain("\"Login,With,Comma\"");
    }

    [Fact]
    public async Task ExportToJsonAsync_EmptyDatabase_ReturnsEmptyJsonArray()
    {
        var bytes = await _sut.ExportToJsonAsync(new AuditExportRequest { MaxRows = 100 });

        var items = JsonSerializer.Deserialize<List<AuditLogDto>>(bytes);
        items.ShouldNotBeNull();
        items!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExportToJsonAsync_WithLogs_ReturnsSerializedList()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "Login"));
        _context.AuditLogs.Add(CreateLog(eventType: "Logout"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bytes = await _sut.ExportToJsonAsync(new AuditExportRequest { MaxRows = 100 });

        var items = JsonSerializer.Deserialize<List<AuditLogDto>>(bytes);
        items.ShouldNotBeNull();
        items!.Count.ShouldBe(2);
        items.Select(i => i.EventType).ShouldContain("Login");
        items.Select(i => i.EventType).ShouldContain("Logout");
    }

    [Fact]
    public async Task ExportToJsonAsync_MaxRowsCapsResult()
    {
        for (var i = 0; i < 6; i++)
        {
            _context.AuditLogs.Add(CreateLog(eventType: $"E{i}"));
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var bytes = await _sut.ExportToJsonAsync(new AuditExportRequest { MaxRows = 3 });

        var items = JsonSerializer.Deserialize<List<AuditLogDto>>(bytes);
        items!.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetStatisticsAsync_EmptyDatabase_ReturnsZeroTotalWithEmptyDictionaries()
    {
        var stats = await _sut.GetStatisticsAsync(from: null, to: null);

        stats.ShouldNotBeNull();
        stats.TotalLogs.ShouldBe(0);
        stats.ByEventType.Count.ShouldBe(0);
        stats.ByHour.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_MultipleLogs_GroupsByEventType()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "Login"));
        _context.AuditLogs.Add(CreateLog(eventType: "Login"));
        _context.AuditLogs.Add(CreateLog(eventType: "Logout"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stats = await _sut.GetStatisticsAsync(from: null, to: null);

        stats.TotalLogs.ShouldBe(3);
        stats.ByEventType["Login"].ShouldBe(2);
        stats.ByEventType["Logout"].ShouldBe(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_DateRange_FiltersLogsCorrectly()
    {
        _context.AuditLogs.Add(CreateLog(eventType: "InRange"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stats = await _sut.GetStatisticsAsync(
            from: DateTime.UtcNow.AddMinutes(-5),
            to: DateTime.UtcNow.AddMinutes(5));

        stats.TotalLogs.ShouldBe(1);
        stats.ByEventType["InRange"].ShouldBe(1);
    }
}
