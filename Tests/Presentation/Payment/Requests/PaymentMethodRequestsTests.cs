using Presentation.Payment.Requests;

namespace Tests.Presentation.Payment.Requests;

public class PaymentMethodRequestsTests
{
    [Fact]
    public void CreatePaymentMethodRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new CreatePaymentMethodRequest("ZarinPal", "zarinpal");

        request.Name.ShouldBe("ZarinPal");
        request.Code.ShouldBe("zarinpal");
        request.Description.ShouldBeNull();
        request.IconUrl.ShouldBeNull();
        request.FeeAmount.ShouldBe(0m);
        request.FeePercentage.ShouldBe(0m);
        request.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void CreatePaymentMethodRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreatePaymentMethodRequest("ZarinPal", "zarinpal", "Desc", "icon.png", 1000, 2.5m, 1);

        request.Description.ShouldBe("Desc");
        request.IconUrl.ShouldBe("icon.png");
        request.FeeAmount.ShouldBe(1000);
        request.FeePercentage.ShouldBe(2.5m);
        request.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void CreatePaymentMethodRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreatePaymentMethodRequest("ZarinPal", "zarinpal");
        var request2 = new CreatePaymentMethodRequest("ZarinPal", "zarinpal");
        var request3 = new CreatePaymentMethodRequest("COD", "cod");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdatePaymentMethodRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdatePaymentMethodRequest("ZarinPal", "Desc", "icon.png", 1000, 2.5m, 3);

        request.Name.ShouldBe("ZarinPal");
        request.Description.ShouldBe("Desc");
        request.IconUrl.ShouldBe("icon.png");
        request.FeeAmount.ShouldBe(1000);
        request.FeePercentage.ShouldBe(2.5m);
        request.SortOrder.ShouldBe(3);
    }

    [Fact]
    public void UpdatePaymentMethodRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdatePaymentMethodRequest("ZarinPal", null, null, 0, 0, 1);
        var request2 = new UpdatePaymentMethodRequest("ZarinPal", null, null, 0, 0, 1);
        var request3 = new UpdatePaymentMethodRequest("COD", null, null, 0, 0, 1);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
