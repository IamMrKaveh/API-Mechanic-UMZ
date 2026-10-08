using Presentation.Payment.Requests;

namespace Tests.Presentation.Payment.Requests;

public class PaymentRequestsTests
{
    [Fact]
    public void AdminPaymentSearchRequest_WithDefaults_SetsCorrectly()
    {
        var request = new AdminPaymentSearchRequest();

        request.OrderId.ShouldBeNull();
        request.UserId.ShouldBeNull();
        request.Status.ShouldBeNull();
        request.Gateway.ShouldBeNull();
        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void AdminPaymentSearchRequest_WithAllParameters_SetsCorrectly()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new AdminPaymentSearchRequest(orderId, userId, "Paid", "ZarinPal", from, to);

        request.OrderId.ShouldBe(orderId);
        request.UserId.ShouldBe(userId);
        request.Status.ShouldBe("Paid");
        request.Gateway.ShouldBe("ZarinPal");
        request.FromDate.ShouldBe(from);
        request.ToDate.ShouldBe(to);
    }

    [Fact]
    public void AdminPaymentSearchRequest_IsRecord_EqualityWorks()
    {
        var request1 = new AdminPaymentSearchRequest(Status: "Paid");
        var request2 = new AdminPaymentSearchRequest(Status: "Paid");
        var request3 = new AdminPaymentSearchRequest(Status: "Failed");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void RefundPaymentRequest_WithReason_SetsCorrectly()
    {
        var request = new RefundPaymentRequest("duplicate charge");

        request.Reason.ShouldBe("duplicate charge");
    }

    [Fact]
    public void RefundPaymentRequest_IsRecord_EqualityWorks()
    {
        var request1 = new RefundPaymentRequest("reason");
        var request2 = new RefundPaymentRequest("reason");
        var request3 = new RefundPaymentRequest("other");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void InitiatePaymentRequest_WithAllParameters_SetsCorrectly()
    {
        var orderId = Guid.NewGuid();

        var request = new InitiatePaymentRequest(orderId, "ZarinPal");

        request.OrderId.ShouldBe(orderId);
        request.Gateway.ShouldBe("ZarinPal");
    }

    [Fact]
    public void InitiatePaymentRequest_IsRecord_EqualityWorks()
    {
        var orderId = Guid.NewGuid();

        var request1 = new InitiatePaymentRequest(orderId, "ZarinPal");
        var request2 = new InitiatePaymentRequest(orderId, "ZarinPal");
        var request3 = new InitiatePaymentRequest(Guid.NewGuid(), "ZarinPal");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void WebhookPayloadRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new WebhookPayloadRequest("AUTH123", "OK");

        request.Authority.ShouldBe("AUTH123");
        request.Status.ShouldBe("OK");
        request.Nonce.ShouldBeNull();
    }

    [Fact]
    public void WebhookPayloadRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new WebhookPayloadRequest("AUTH123", "OK", "nonce-1");

        request.Authority.ShouldBe("AUTH123");
        request.Status.ShouldBe("OK");
        request.Nonce.ShouldBe("nonce-1");
    }

    [Fact]
    public void WebhookPayloadRequest_IsRecord_EqualityWorks()
    {
        var request1 = new WebhookPayloadRequest("AUTH123", "OK", "nonce-1");
        var request2 = new WebhookPayloadRequest("AUTH123", "OK", "nonce-1");
        var request3 = new WebhookPayloadRequest("AUTH123", "NOK", "nonce-1");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
