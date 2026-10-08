using Presentation.Wallet.Requests;

namespace Tests.Presentation.Wallet.Requests;

public class FraudAlertRequestsTests
{
    [Fact]
    public void FraudAlertReviewRequest_WithNote_SetsCorrectly()
    {
        var request = new FraudAlertReviewRequest("checked");

        request.Note.ShouldBe("checked");
    }

    [Fact]
    public void FraudAlertReviewRequest_WithNullNote_SetsCorrectly()
    {
        var request = new FraudAlertReviewRequest(null);

        request.Note.ShouldBeNull();
    }

    [Fact]
    public void FraudAlertDismissRequest_WithNote_SetsCorrectly()
    {
        var request = new FraudAlertDismissRequest("false positive");

        request.Note.ShouldBe("false positive");
    }

    [Fact]
    public void GetFraudAlertsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetFraudAlertsRequest();

        request.Status.ShouldBeNull();
        request.Severity.ShouldBeNull();
        request.UserId.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetFraudAlertsRequest_WithAllParameters_SetsCorrectly()
    {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetFraudAlertsRequest("Open", "High", userId, 2, 15, from, to);

        request.Status.ShouldBe("Open");
        request.Severity.ShouldBe("High");
        request.UserId.ShouldBe(userId);
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(15);
        request.FromDate.ShouldBe(from);
        request.ToDate.ShouldBe(to);
    }

    [Fact]
    public void GetFraudAlertsRequest_IsRecord_EqualityWorks()
    {
        var request1 = new GetFraudAlertsRequest(Status: "Open");
        var request2 = new GetFraudAlertsRequest(Status: "Open");
        var request3 = new GetFraudAlertsRequest(Status: "Reviewed");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
