using Presentation.Wallet.Requests;

namespace Tests.Presentation.Wallet.Requests;

public class WalletTransferRequestsTests
{
    [Fact]
    public void PreviewWalletTransferRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new PreviewWalletTransferRequest("09123456789", 50000);

        request.RecipientPhoneNumber.ShouldBe("09123456789");
        request.Amount.ShouldBe(50000);
    }

    [Fact]
    public void InitiateWalletTransferRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new InitiateWalletTransferRequest("09123456789", 50000, "gift");

        request.RecipientPhoneNumber.ShouldBe("09123456789");
        request.Amount.ShouldBe(50000);
        request.Description.ShouldBe("gift");
    }

    [Fact]
    public void ConfirmWalletTransferRequest_WithAllParameters_SetsCorrectly()
    {
        var transferId = Guid.NewGuid();

        var request = new ConfirmWalletTransferRequest(transferId, "123456");

        request.TransferId.ShouldBe(transferId);
        request.OtpCode.ShouldBe("123456");
    }

    [Fact]
    public void CancelWalletTransferRequest_WithTransferId_SetsCorrectly()
    {
        var transferId = Guid.NewGuid();

        var request = new CancelWalletTransferRequest(transferId);

        request.TransferId.ShouldBe(transferId);
    }

    [Fact]
    public void PreviewWalletTransferRequest_IsRecord_EqualityWorks()
    {
        var request1 = new PreviewWalletTransferRequest("09123456789", 50000);
        var request2 = new PreviewWalletTransferRequest("09123456789", 50000);
        var request3 = new PreviewWalletTransferRequest("09987654321", 50000);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
