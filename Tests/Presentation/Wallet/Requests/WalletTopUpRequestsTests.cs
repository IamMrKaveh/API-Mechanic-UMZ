using Presentation.Wallet.Requests;

namespace Tests.Presentation.Wallet.Requests;

public class WalletTopUpRequestsTests
{
    [Fact]
    public void CompleteTopUpRequest_WithNulls_SetsCorrectly()
    {
        var request = new CompleteTopUpRequest(null, null);

        request.Authority.ShouldBeNull();
        request.Status.ShouldBeNull();
    }

    [Fact]
    public void CompleteTopUpRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CompleteTopUpRequest("AUTH1", "OK");

        request.Authority.ShouldBe("AUTH1");
        request.Status.ShouldBe("OK");
    }

    [Fact]
    public void CompleteTopUpRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CompleteTopUpRequest("AUTH1", "OK");
        var request2 = new CompleteTopUpRequest("AUTH1", "OK");
        var request3 = new CompleteTopUpRequest("AUTH2", "OK");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void InitiateTopUpRequest_WithDefaults_UsesZarinpal()
    {
        var request = new InitiateTopUpRequest(100000);

        request.Amount.ShouldBe(100000);
        request.Gateway.ShouldBe("zarinpal");
    }
}
