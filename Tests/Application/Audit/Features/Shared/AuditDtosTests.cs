using Application.Audit.Features.Shared;

namespace Tests.Application.Audit.Features.Shared;

public class AuditDtosTests
{
    [Fact]
    public void AuditLogDto_Defaults_AreEmpty()
    {
        var dto = new AuditLogDto();

        dto.Id.ShouldBe(default(Guid));
        dto.UserId.ShouldBeNull();
        dto.UserName.ShouldBeNull();
        dto.EventType.ShouldBe(string.Empty);
        dto.Action.ShouldBe(string.Empty);
        dto.Details.ShouldBeNull();
        dto.IpAddress.ShouldBeNull();
        dto.UserAgent.ShouldBeNull();
        dto.EntityType.ShouldBeNull();
        dto.EntityId.ShouldBeNull();
        dto.CreatedAt.ShouldBe(default);
        dto.IsArchived.ShouldBeFalse();
    }

    [Fact]
    public void AuditLogDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var dto = new AuditLogDto
        {
            Id = id,
            UserId = userId,
            UserName = "09120000000",
            EventType = "Security",
            Action = "Login",
            Details = "ok",
            IpAddress = "1.2.3.4",
            UserAgent = "ua",
            EntityType = "User",
            EntityId = userId.ToString(),
            CreatedAt = createdAt,
            IsArchived = true
        };

        dto.Id.ShouldBe(id);
        dto.UserId.ShouldBe(userId);
        dto.EventType.ShouldBe("Security");
        dto.IsArchived.ShouldBeTrue();
        dto.CreatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void AuditStatisticsDto_DefaultDictionaries_AreEmpty()
    {
        var dto = new AuditStatisticsDto();

        dto.TotalLogs.ShouldBe(0);
        dto.ByEventType.ShouldNotBeNull();
        dto.ByEventType.ShouldBeEmpty();
        dto.ByHour.ShouldNotBeNull();
        dto.ByHour.ShouldBeEmpty();
    }

    [Fact]
    public void AuditStatisticsDto_WithData_PreservesCounts()
    {
        var dto = new AuditStatisticsDto
        {
            TotalLogs = 10,
            ByEventType = new Dictionary<string, long> { ["Login"] = 7, ["Logout"] = 3 },
            ByHour = new Dictionary<string, long> { ["09"] = 5, ["10"] = 5 }
        };

        dto.TotalLogs.ShouldBe(10);
        dto.ByEventType["Login"].ShouldBe(7);
        dto.ByHour["10"].ShouldBe(5);
    }

    [Fact]
    public void ExportAuditLogsResult_PositionalEquality_Works()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var a = new ExportAuditLogsResult(bytes, "audit.csv", "text/csv");
        var b = new ExportAuditLogsResult(bytes, "audit.csv", "text/csv");

        a.ShouldBe(b);
        a.FileName.ShouldBe("audit.csv");
        a.ContentType.ShouldBe("text/csv");
    }

    [Fact]
    public void EventTypeCountDto_StoresValues()
    {
        var dto = new EventTypeCountDto("Login", 4);

        dto.EventType.ShouldBe("Login");
        dto.Count.ShouldBe(4);
    }

    [Fact]
    public void HourlyCountDto_StoresValues()
    {
        var dto = new HourlyCountDto(9, 11);

        dto.Hour.ShouldBe(9);
        dto.Count.ShouldBe(11);
    }

    [Fact]
    public void AuditExportRequest_DefaultMaxRows_IsZero()
    {
        var dto = new AuditExportRequest();

        dto.UserId.ShouldBeNull();
        dto.Action.ShouldBeNull();
        dto.EntityType.ShouldBeNull();
        dto.EventType.ShouldBeNull();
        dto.From.ShouldBeNull();
        dto.To.ShouldBeNull();
        dto.MaxRows.ShouldBe(0);
    }

    [Fact]
    public void AuditSearchRequest_DefaultPaging_IsOneAndTen()
    {
        var dto = new AuditSearchRequest();

        dto.Page.ShouldBe(1);
        dto.PageSize.ShouldBe(10);
        dto.SortDesc.ShouldBeFalse();
        dto.Keyword.ShouldBeNull();
        dto.SortBy.ShouldBeNull();
    }

    [Fact]
    public void AuditSearchRequest_WithExpression_PreservesOthers()
    {
        var dto = new AuditSearchRequest { Page = 2, PageSize = 20, Keyword = "login" };

        var updated = dto with { Page = 3 };

        updated.Page.ShouldBe(3);
        updated.PageSize.ShouldBe(20);
        updated.Keyword.ShouldBe("login");
    }
}
