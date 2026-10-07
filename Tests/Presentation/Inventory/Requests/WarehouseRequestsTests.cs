using Presentation.Inventory.Requests;

namespace Tests.Presentation.Inventory.Requests;

public class WarehouseRequestsTests
{
    [Fact]
    public void CreateWarehouseRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateWarehouseRequest("WH-1", "Main", "Tehran", "Addr", "021", 1, true);

        request.Code.ShouldBe("WH-1");
        request.Name.ShouldBe("Main");
        request.City.ShouldBe("Tehran");
        request.Address.ShouldBe("Addr");
        request.Phone.ShouldBe("021");
        request.Priority.ShouldBe(1);
        request.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void CreateWarehouseRequest_WithDefaultIsDefault_SetsToFalse()
    {
        var request = new CreateWarehouseRequest("WH-1", "Main", "Tehran", null, null, 1);

        request.IsDefault.ShouldBeFalse();
        request.Address.ShouldBeNull();
        request.Phone.ShouldBeNull();
    }

    [Fact]
    public void CreateWarehouseRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreateWarehouseRequest("WH-1", "Main", "Tehran", null, null, 1, false);
        var request2 = new CreateWarehouseRequest("WH-1", "Main", "Tehran", null, null, 1, false);
        var request3 = new CreateWarehouseRequest("WH-2", "Main", "Tehran", null, null, 1, false);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateWarehouseRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateWarehouseRequest("Main", "Tehran", "Addr", "021", 2);

        request.Name.ShouldBe("Main");
        request.City.ShouldBe("Tehran");
        request.Address.ShouldBe("Addr");
        request.Phone.ShouldBe("021");
        request.Priority.ShouldBe(2);
    }

    [Fact]
    public void UpdateWarehouseRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateWarehouseRequest("Main", "Tehran", null, null, 1);
        var request2 = new UpdateWarehouseRequest("Main", "Tehran", null, null, 1);
        var request3 = new UpdateWarehouseRequest("Other", "Tehran", null, null, 1);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void ToggleWarehouseStatusRequest_WithIsActive_SetsCorrectly()
    {
        var active = new ToggleWarehouseStatusRequest(true);
        var inactive = new ToggleWarehouseStatusRequest(false);

        active.IsActive.ShouldBeTrue();
        inactive.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void ToggleWarehouseStatusRequest_IsRecord_EqualityWorks()
    {
        var request1 = new ToggleWarehouseStatusRequest(true);
        var request2 = new ToggleWarehouseStatusRequest(true);
        var request3 = new ToggleWarehouseStatusRequest(false);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
