using Presentation.Audit.Requests;

namespace Tests.Presentation.Audit.Requests;

public class AuditRequestsTests
{
    [Fact]
    public void GetAuditLogsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAuditLogsRequest();

        request.UserId.ShouldBeNull();
        request.EventType.ShouldBeNull();
        request.EntityType.ShouldBeNull();
        request.Action.ShouldBeNull();
        request.Keyword.ShouldBeNull();
        request.IpAddress.ShouldBeNull();
        request.From.ShouldBeNull();
        request.To.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(50);
        request.SortBy.ShouldBe("CreatedAt");
        request.SortDesc.ShouldBeTrue();
    }

    [Fact]
    public void GetAuditLogsRequest_WithAllParameters_SetsCorrectly()
    {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetAuditLogsRequest(userId, "Login", "User", "Login", "admin", "127.0.0.1", from, to, 2, 25, "Action", false);

        request.UserId.ShouldBe(userId);
        request.EventType.ShouldBe("Login");
        request.EntityType.ShouldBe("User");
        request.Action.ShouldBe("Login");
        request.Keyword.ShouldBe("admin");
        request.IpAddress.ShouldBe("127.0.0.1");
        request.From.ShouldBe(from);
        request.To.ShouldBe(to);
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(25);
        request.SortBy.ShouldBe("Action");
        request.SortDesc.ShouldBeFalse();
    }

    [Fact]
    public void GetAuditLogsRequest_IsRecord_EqualityWorks()
    {
        var request1 = new GetAuditLogsRequest(EventType: "Login");
        var request2 = new GetAuditLogsRequest(EventType: "Login");
        var request3 = new GetAuditLogsRequest(EventType: "Logout");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void GetAuditStatisticsRequest_WithDefaults_HasNullDates()
    {
        var request = new GetAuditStatisticsRequest();

        request.From.ShouldBeNull();
        request.To.ShouldBeNull();
    }

    [Fact]
    public void GetAuditStatisticsRequest_WithAllParameters_SetsCorrectly()
    {
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetAuditStatisticsRequest(from, to);

        request.From.ShouldBe(from);
        request.To.ShouldBe(to);
    }

    [Fact]
    public void GetAuditStatisticsRequest_IsRecord_EqualityWorks()
    {
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        var request1 = new GetAuditStatisticsRequest(from, null);
        var request2 = new GetAuditStatisticsRequest(from, null);
        var request3 = new GetAuditStatisticsRequest(null, from);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void ExportAuditLogsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new ExportAuditLogsRequest();

        request.UserId.ShouldBeNull();
        request.EventType.ShouldBeNull();
        request.EntityType.ShouldBeNull();
        request.From.ShouldBeNull();
        request.To.ShouldBeNull();
        request.MaxRows.ShouldBeNull();
    }

    [Fact]
    public void ExportAuditLogsRequest_WithAllParameters_SetsCorrectly()
    {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new ExportAuditLogsRequest(userId, "Login", "User", from, to, 100);

        request.UserId.ShouldBe(userId);
        request.EventType.ShouldBe("Login");
        request.EntityType.ShouldBe("User");
        request.From.ShouldBe(from);
        request.To.ShouldBe(to);
        request.MaxRows.ShouldBe(100);
    }

    [Fact]
    public void ExportAuditLogsRequest_IsRecord_EqualityWorks()
    {
        var request1 = new ExportAuditLogsRequest(MaxRows: 100);
        var request2 = new ExportAuditLogsRequest(MaxRows: 100);
        var request3 = new ExportAuditLogsRequest(MaxRows: 50);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
