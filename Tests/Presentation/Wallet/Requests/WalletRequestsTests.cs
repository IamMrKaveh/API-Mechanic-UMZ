using Domain.Wallet.Enums;
using Presentation.Wallet.Requests;

namespace Tests.Presentation.Wallet.Requests;

public class WalletRequestsTests
{
    [Fact]
    public void AdminWalletAdjustmentRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new AdminWalletAdjustmentRequest(50000, "bonus");

        request.Amount.ShouldBe(50000);
        request.Reason.ShouldBe("bonus");
        request.Description.ShouldBeNull();
        request.TransactionType.ShouldBe(AdminWalletAdjustmentType.AdminAdjustment);
        request.ReferenceId.ShouldBeNull();
    }

    [Fact]
    public void AdminWalletAdjustmentRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new AdminWalletAdjustmentRequest(50000, "bonus", "note", AdminWalletAdjustmentType.Refund, "ref-1");

        request.Description.ShouldBe("note");
        request.TransactionType.ShouldBe(AdminWalletAdjustmentType.Refund);
        request.ReferenceId.ShouldBe("ref-1");
    }

    [Fact]
    public void CreditWalletRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreditWalletRequest(10000, "desc", "ref-1", "key-1");

        request.Amount.ShouldBe(10000);
        request.Description.ShouldBe("desc");
        request.ReferenceId.ShouldBe("ref-1");
        request.IdempotencyKey.ShouldBe("key-1");
    }

    [Fact]
    public void DebitWalletRequest_WithDefaults_SetsIdempotencyKeyToNull()
    {
        var request = new DebitWalletRequest(10000, "desc", "ref-1");

        request.IdempotencyKey.ShouldBeNull();
    }

    [Fact]
    public void ReserveWalletRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new ReserveWalletRequest(5000, "order hold");

        request.Amount.ShouldBe(5000);
        request.Purpose.ShouldBe("order hold");
    }

    [Fact]
    public void RequestWithdrawalRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new RequestWithdrawalRequest(50000, "IR123", "Ali");

        request.Amount.ShouldBe(50000);
        request.Iban.ShouldBe("IR123");
        request.AccountHolder.ShouldBe("Ali");
        request.Description.ShouldBeNull();
    }

    [Fact]
    public void RejectWithdrawalRequest_WithReason_SetsCorrectly()
    {
        var request = new RejectWithdrawalRequest("invalid iban");

        request.Reason.ShouldBe("invalid iban");
    }

    [Fact]
    public void MarkWithdrawalPaidRequest_WithReference_SetsCorrectly()
    {
        var request = new MarkWithdrawalPaidRequest("REF1");

        request.BankReferenceNumber.ShouldBe("REF1");
    }

    [Fact]
    public void GetWithdrawalsListRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetWithdrawalsListRequest();

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
    }

    [Fact]
    public void GetPendingWithdrawalsListRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetPendingWithdrawalsListRequest();

        request.Status.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetWalletLedgerRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetWalletLedgerRequest();

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
    }

    [Fact]
    public void FreezeWalletRequest_WithReason_SetsCorrectly()
    {
        var request = new FreezeWalletRequest("suspicious");

        request.Reason.ShouldBe("suspicious");
    }

    [Fact]
    public void GetWalletsOverviewRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetWalletsOverviewRequest();

        request.Search.ShouldBeNull();
        request.IsFrozen.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
    }

    [Fact]
    public void AdminImmediateDebitRequest_WithDefaults_SetsCorrectly()
    {
        var request = new AdminImmediateDebitRequest(10000, "penalty");

        request.Amount.ShouldBe(10000);
        request.Reason.ShouldBe("penalty");
        request.ConfirmForceDebit.ShouldBeFalse();
    }

    [Fact]
    public void AdminImmediateDebitRequest_WithConfirmation_SetsCorrectly()
    {
        var request = new AdminImmediateDebitRequest(10000, "penalty", null, null, true);

        request.ConfirmForceDebit.ShouldBeTrue();
    }

    [Fact]
    public void GetAdminTransfersRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAdminTransfersRequest();

        request.UserId.ShouldBeNull();
        request.Status.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
    }

    [Fact]
    public void GetAdminDebitRequestsListRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAdminDebitRequestsListRequest();

        request.OwnerId.ShouldBeNull();
        request.RequestedBy.ShouldBeNull();
        request.Status.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
    }

    [Fact]
    public void GetAdminWalletLedgerRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAdminWalletLedgerRequest();

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
        request.FromDate.ShouldBeNull();
        request.TransactionType.ShouldBeNull();
    }

    [Fact]
    public void ExportAdminWalletLedgerRequest_WithDefaults_SetsCorrectly()
    {
        var request = new ExportAdminWalletLedgerRequest();

        request.Format.ShouldBe("csv");
        request.MaxRows.ShouldBeNull();
    }

    [Fact]
    public void ForceFreezeFromFraudAlertRequest_WithDefaults_SetsNoteToNull()
    {
        var request = new ForceFreezeFromFraudAlertRequest();

        request.AdditionalNote.ShouldBeNull();
    }

    [Fact]
    public void AdminWalletAdjustmentRequest_IsRecord_EqualityWorks()
    {
        var request1 = new AdminWalletAdjustmentRequest(50000, "bonus");
        var request2 = new AdminWalletAdjustmentRequest(50000, "bonus");
        var request3 = new AdminWalletAdjustmentRequest(60000, "bonus");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
