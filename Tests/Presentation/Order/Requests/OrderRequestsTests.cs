using Presentation.Order.Requests;

namespace Tests.Presentation.Order.Requests;

public class OrderRequestsTests
{
    [Fact]
    public void GetAdminOrdersRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAdminOrdersRequest();

        request.UserId.ShouldBeNull();
        request.Status.ShouldBeNull();
        request.IsPaid.ShouldBeNull();
        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
    }

    [Fact]
    public void GetAdminOrdersRequest_WithAllParameters_SetsCorrectly()
    {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetAdminOrdersRequest(userId, "Pending", true, from, to, 2, 25);

        request.UserId.ShouldBe(userId);
        request.Status.ShouldBe("Pending");
        request.IsPaid.ShouldBe(true);
        request.FromDate.ShouldBe(from);
        request.ToDate.ShouldBe(to);
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(25);
    }

    [Fact]
    public void GetUserOrdersRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetUserOrdersRequest();

        request.Status.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
    }

    [Fact]
    public void GetUserOrdersRequest_IsRecord_EqualityWorks()
    {
        var request1 = new GetUserOrdersRequest("Pending", 1, 10);
        var request2 = new GetUserOrdersRequest("Pending", 1, 10);
        var request3 = new GetUserOrdersRequest("Shipped", 1, 10);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void GetOrderStatusesRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetOrderStatusesRequest();

        request.OnlyActive.ShouldBeNull();
    }

    [Fact]
    public void GetOrderStatusesRequest_WithValue_SetsCorrectly()
    {
        var request = new GetOrderStatusesRequest(true);

        request.OnlyActive.ShouldBe(true);
    }

    [Fact]
    public void GetOrderStatisticsRequest_WithDefaults_HasNullDates()
    {
        var request = new GetOrderStatisticsRequest();

        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void CheckoutFromCartRequest_WithRequiredValues_SetsCorrectly()
    {
        var cartId = Guid.NewGuid();
        var shippingId = Guid.NewGuid();
        var addressId = Guid.NewGuid();

        var request = new CheckoutFromCartRequest(cartId, shippingId, addressId);

        request.CartId.ShouldBe(cartId);
        request.ShippingId.ShouldBe(shippingId);
        request.AddressId.ShouldBe(addressId);
        request.DiscountCode.ShouldBeNull();
        request.PaymentGateway.ShouldBeNull();
        request.PaymentMethodId.ShouldBeNull();
    }

    [Fact]
    public void CheckoutFromCartRequest_WithAllParameters_SetsCorrectly()
    {
        var cartId = Guid.NewGuid();
        var shippingId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();

        var request = new CheckoutFromCartRequest(cartId, shippingId, addressId, "SAVE10", "ZarinPal", paymentMethodId);

        request.DiscountCode.ShouldBe("SAVE10");
        request.PaymentGateway.ShouldBe("ZarinPal");
        request.PaymentMethodId.ShouldBe(paymentMethodId);
    }

    [Fact]
    public void CancelOrderRequest_WithReason_SetsCorrectly()
    {
        var request = new CancelOrderRequest("changed mind");

        request.Reason.ShouldBe("changed mind");
    }

    [Fact]
    public void RequestReturnRequest_WithReason_SetsCorrectly()
    {
        var request = new RequestReturnRequest("defective");

        request.Reason.ShouldBe("defective");
    }

    [Fact]
    public void UpdateOrderStatusByIdRequest_WithNewStatus_SetsCorrectly()
    {
        var request = new UpdateOrderStatusByIdRequest("Shipped");

        request.NewStatus.ShouldBe("Shipped");
    }

    [Fact]
    public void CreateOrderStatusRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateOrderStatusRequest("pending", "Pending", "icon", "#fff", 1, true, false);

        request.Name.ShouldBe("pending");
        request.DisplayName.ShouldBe("Pending");
        request.Icon.ShouldBe("icon");
        request.Color.ShouldBe("#fff");
        request.SortOrder.ShouldBe(1);
        request.AllowCancel.ShouldBeTrue();
        request.AllowEdit.ShouldBeFalse();
    }

    [Fact]
    public void UpdateOrderStatusRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateOrderStatusRequest("Pending", "icon", "#fff", 2, true, true);

        request.DisplayName.ShouldBe("Pending");
        request.Icon.ShouldBe("icon");
        request.Color.ShouldBe("#fff");
        request.SortOrder.ShouldBe(2);
        request.AllowCancel.ShouldBeTrue();
        request.AllowEdit.ShouldBeTrue();
    }

    [Fact]
    public void AdminCreateOrderRequest_WithAllParameters_SetsCorrectly()
    {
        var userId = Guid.NewGuid();
        var shippingId = Guid.NewGuid();
        var items = new List<OrderItemRequest> { new(Guid.NewGuid(), 2) };

        var request = new AdminCreateOrderRequest(userId, shippingId, "Name", "09123456789", "Tehran", "Tehran", "Street", "12345", items);

        request.UserId.ShouldBe(userId);
        request.ShippingId.ShouldBe(shippingId);
        request.ReceiverFullName.ShouldBe("Name");
        request.Items.Count.ShouldBe(1);
    }

    [Fact]
    public void OrderItemRequest_WithAllParameters_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();

        var item = new OrderItemRequest(variantId, 2);

        item.VariantId.ShouldBe(variantId);
        item.Quantity.ShouldBe(2);
    }
}
