using Presentation.Wallet.Requests;

namespace Tests.Presentation.Wallet.Requests;

public class WalletDebitRequestPayloadsTests
{
    [Fact]
    public void AdminWalletDebitRequestPayload_WithRequiredValues_SetsCorrectly()
    {
        var request = new AdminWalletDebitRequestPayload(20000, "correction");

        request.Amount.ShouldBe(20000);
        request.Reason.ShouldBe("correction");
        request.Description.ShouldBeNull();
        request.ExpiryHours.ShouldBe(72);
        request.ReferenceId.ShouldBeNull();
    }

    [Fact]
    public void AdminWalletDebitRequestPayload_WithAllParameters_SetsCorrectly()
    {
        var request = new AdminWalletDebitRequestPayload(20000, "correction", "note", 48, "ref-1");

        request.Description.ShouldBe("note");
        request.ExpiryHours.ShouldBe(48);
        request.ReferenceId.ShouldBe("ref-1");
    }

    [Fact]
    public void AdminWalletDebitRequestPayload_IsRecord_EqualityWorks()
    {
        var request1 = new AdminWalletDebitRequestPayload(20000, "correction");
        var request2 = new AdminWalletDebitRequestPayload(20000, "correction");
        var request3 = new AdminWalletDebitRequestPayload(30000, "correction");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void RejectWalletDebitRequest_WithReason_SetsCorrectly()
    {
        var request = new RejectWalletDebitRequest("insufficient docs");

        request.RejectionReason.ShouldBe("insufficient docs");
    }
}
