using Presentation.Shipping.Requests;

namespace Tests.Presentation.Shipping.Requests;

public class ShippingRequestsTests
{
    [Fact]
    public void CreateShippingRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new CreateShippingRequest("Post", 50000);

        request.Name.ShouldBe("Post");
        request.BaseCost.ShouldBe(50000);
        request.Description.ShouldBeNull();
        request.EstimatedDeliveryTime.ShouldBeNull();
        request.MinDeliveryDays.ShouldBe(1);
        request.MaxDeliveryDays.ShouldBe(7);
    }

    [Fact]
    public void CreateShippingRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateShippingRequest("Post", 50000, "Desc", "2-3 days", 2, 5);

        request.Description.ShouldBe("Desc");
        request.EstimatedDeliveryTime.ShouldBe("2-3 days");
        request.MinDeliveryDays.ShouldBe(2);
        request.MaxDeliveryDays.ShouldBe(5);
    }

    [Fact]
    public void CreateShippingRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreateShippingRequest("Post", 50000);
        var request2 = new CreateShippingRequest("Post", 50000);
        var request3 = new CreateShippingRequest("Courier", 50000);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateShippingRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateShippingRequest("Post", 60000, "Desc", "3-4 days", 2, 4);

        request.Name.ShouldBe("Post");
        request.BaseCost.ShouldBe(60000);
        request.Description.ShouldBe("Desc");
        request.EstimatedDeliveryTime.ShouldBe("3-4 days");
        request.MinDeliveryDays.ShouldBe(2);
        request.MaxDeliveryDays.ShouldBe(4);
    }

    [Fact]
    public void UpdateShippingRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateShippingRequest("Post", 50000, null, null, 1, 7);
        var request2 = new UpdateShippingRequest("Post", 50000, null, null, 1, 7);
        var request3 = new UpdateShippingRequest("Post", 60000, null, null, 1, 7);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
