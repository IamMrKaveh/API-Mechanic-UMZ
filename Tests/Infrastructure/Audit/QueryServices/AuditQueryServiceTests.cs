using Application.Audit.Features.Shared;
using Domain.Audit.Entities;
using Domain.User.ValueObjects;
using Infrastructure.Audit.QueryServices;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Audit.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AuditQueryServiceTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private AuditQueryService _sut = null!;

    protected override Task OnInitializeAsync()
    {
        _sut = new AuditQueryService(Context);
        return Task.CompletedTask;
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
        Context.AuditLogs.AddRange(
            CreateLog(),
            CreateLog(),
            CreateLog());
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest { Page = 1, PageSize = 10 });

        total.ShouldBe(3);
        logs.Count.ShouldBe(3);
    }

    [Fact]
    public async Task SearchAsync_FilteredByUserId_ReturnsMatchingOnly()
    {
        var targetUser = await SeedUserAsync();
        var otherUser = await SeedUserAsync();

        Context.AuditLogs.Add(CreateLog(userId: targetUser.Id, eventType: "UserLogin"));
        Context.AuditLogs.Add(CreateLog(userId: otherUser.Id, eventType: "UserLogin"));
        Context.AuditLogs.Add(CreateLog(userId: null, eventType: "AnonymousLogin"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var (logs, total) = await _sut.SearchAsync(new AuditSearchRequest
        {
            UserId = targetUser.Id.Value,
            Page = 1,
            PageSize = 10
        });

        total.ShouldBe(1);
        logs.Count.ShouldBe(1);
        logs[0].UserId.ShouldBe(targetUser.Id.Value);
    }

    [Fact]
    public async Task SearchAsync_FilteredByEventType_ReturnsMatchingOnly()
    {
        Context.AuditLogs.Add(CreateLog(eventType: "UserLogin"));
        Context.AuditLogs.Add(CreateLog(eventType: "UserLogout"));
        Context.AuditLogs.Add(CreateLog(eventType: "OrderPlaced"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(entityType: "Order"));
        Context.AuditLogs.Add(CreateLog(entityType: "Product"));
        Context.AuditLogs.Add(CreateLog(entityType: "User"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(action: "Create"));
        Context.AuditLogs.Add(CreateLog(action: "Delete"));
        Context.AuditLogs.Add(CreateLog(action: "Update"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(details: "user reset password successfully"));
        Context.AuditLogs.Add(CreateLog(details: "user updated profile"));
        Context.AuditLogs.Add(CreateLog(details: "unrelated activity"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(ipAddress: "10.0.0.1"));
        Context.AuditLogs.Add(CreateLog(ipAddress: "10.0.0.2"));
        Context.AuditLogs.Add(CreateLog(ipAddress: "10.0.0.3"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(eventType: "Bravo"));
        Context.AuditLogs.Add(CreateLog(eventType: "Alpha"));
        Context.AuditLogs.Add(CreateLog(eventType: "Charlie"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(action: "Zeta"));
        Context.AuditLogs.Add(CreateLog(action: "Alpha"));
        Context.AuditLogs.Add(CreateLog(action: "Mike"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(eventType: "First"));
        await Context.SaveChangesAsync();
        await Task.Delay(30);
        Context.AuditLogs.Add(CreateLog(eventType: "Second"));
        await Context.SaveChangesAsync();
        await Task.Delay(30);
        Context.AuditLogs.Add(CreateLog(eventType: "Third"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
            Context.AuditLogs.Add(CreateLog(eventType: $"Evt{i}"));
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(eventType: "Login"));
        Context.AuditLogs.Add(CreateLog(eventType: "Logout"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
            Context.AuditLogs.Add(CreateLog(eventType: $"Ev{i}"));
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var bytes = await _sut.ExportToCsvAsync(new AuditExportRequest { MaxRows = 2 });

        var csv = Encoding.UTF8.GetString(bytes);
        var lines = csv.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(3);
    }

    [Fact]
    public async Task ExportToCsvAsync_ValuesWithCommas_ArePropertyEscaped()
    {
        Context.AuditLogs.Add(CreateLog(eventType: "Login,With,Comma", action: "OK"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(eventType: "Login"));
        Context.AuditLogs.Add(CreateLog(eventType: "Logout"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
            Context.AuditLogs.Add(CreateLog(eventType: $"E{i}"));
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        Context.AuditLogs.Add(CreateLog(eventType: "Login"));
        Context.AuditLogs.Add(CreateLog(eventType: "Login"));
        Context.AuditLogs.Add(CreateLog(eventType: "Logout"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var stats = await _sut.GetStatisticsAsync(from: null, to: null);

        stats.TotalLogs.ShouldBe(3);
        stats.ByEventType["Login"].ShouldBe(2);
        stats.ByEventType["Logout"].ShouldBe(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_DateRange_FiltersLogsCorrectly()
    {
        Context.AuditLogs.Add(CreateLog(eventType: "InRange"));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var stats = await _sut.GetStatisticsAsync(
            from: DateTime.UtcNow.AddMinutes(-5),
            to: DateTime.UtcNow.AddMinutes(5));

        stats.TotalLogs.ShouldBe(1);
        stats.ByEventType["InRange"].ShouldBe(1);
    }
}
