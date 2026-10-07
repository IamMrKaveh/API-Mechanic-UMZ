using Presentation.Discount.Requests;

namespace Tests.Presentation.Discount.Requests;

public class DiscountRequestsTests
{
    [Fact]
    public void CreateDiscountRequest_WithAllParameters_SetsCorrectly()
    {
        var startsAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var expiresAt = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new CreateDiscountRequest("SAVE10", "Percentage", 10, 50000, 100, startsAt, expiresAt);

        request.Code.ShouldBe("SAVE10");
        request.DiscountType.ShouldBe("Percentage");
        request.DiscountValue.ShouldBe(10);
        request.MaximumDiscountAmount.ShouldBe(50000);
        request.UsageLimit.ShouldBe(100);
        request.StartsAt.ShouldBe(startsAt);
        request.ExpiresAt.ShouldBe(expiresAt);
    }

    [Fact]
    public void CreateDiscountRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreateDiscountRequest("SAVE10", "Percentage", 10, null, null, null, null);
        var request2 = new CreateDiscountRequest("SAVE10", "Percentage", 10, null, null, null, null);
        var request3 = new CreateDiscountRequest("SAVE20", "Percentage", 20, null, null, null, null);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateDiscountRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateDiscountRequest("FixedAmount", 5000, 20000, 50, null, null, false);

        request.DiscountType.ShouldBe("FixedAmount");
        request.DiscountValue.ShouldBe(5000);
        request.MaximumDiscountAmount.ShouldBe(20000);
        request.UsageLimit.ShouldBe(50);
        request.StartsAt.ShouldBeNull();
        request.ExpiresAt.ShouldBeNull();
        request.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void UpdateDiscountRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateDiscountRequest("Percentage", 10, null, null, null, null, true);
        var request2 = new UpdateDiscountRequest("Percentage", 10, null, null, null, null, true);
        var request3 = new UpdateDiscountRequest("Percentage", 15, null, null, null, null, true);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void ValidateDiscountRequest_WithDefaults_UsesIrtCurrency()
    {
        var request = new ValidateDiscountRequest("SAVE10", 1000);

        request.Code.ShouldBe("SAVE10");
        request.OrderAmount.ShouldBe(1000);
        request.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void ValidateDiscountRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new ValidateDiscountRequest("SAVE10", 1000, "USD");

        request.Code.ShouldBe("SAVE10");
        request.OrderAmount.ShouldBe(1000);
        request.Currency.ShouldBe("USD");
    }

    [Fact]
    public void ValidateDiscountRequest_IsRecord_EqualityWorks()
    {
        var request1 = new ValidateDiscountRequest("SAVE10", 1000, "IRT");
        var request2 = new ValidateDiscountRequest("SAVE10", 1000, "IRT");
        var request3 = new ValidateDiscountRequest("SAVE20", 1000, "IRT");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void ApplyDiscountRequest_WithAllParameters_SetsCorrectly()
    {
        var orderId = Guid.NewGuid();

        var request = new ApplyDiscountRequest("SAVE10", orderId, 1000);

        request.Code.ShouldBe("SAVE10");
        request.OrderId.ShouldBe(orderId);
        request.OrderAmount.ShouldBe(1000);
    }

    [Fact]
    public void ApplyDiscountRequest_IsRecord_EqualityWorks()
    {
        var orderId = Guid.NewGuid();

        var request1 = new ApplyDiscountRequest("SAVE10", orderId, 1000);
        var request2 = new ApplyDiscountRequest("SAVE10", orderId, 1000);
        var request3 = new ApplyDiscountRequest("SAVE10", Guid.NewGuid(), 1000);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void CancelDiscountUsageRequest_WithOrderId_SetsCorrectly()
    {
        var orderId = Guid.NewGuid();

        var request = new CancelDiscountUsageRequest(orderId);

        request.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void CancelDiscountUsageRequest_IsRecord_EqualityWorks()
    {
        var orderId = Guid.NewGuid();

        var request1 = new CancelDiscountUsageRequest(orderId);
        var request2 = new CancelDiscountUsageRequest(orderId);
        var request3 = new CancelDiscountUsageRequest(Guid.NewGuid());

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
