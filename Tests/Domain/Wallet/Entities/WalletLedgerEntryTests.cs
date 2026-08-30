using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Wallet.Entities;

public class WalletLedgerEntryTests
{
    private static Money Rial(decimal amount) => Money.Create(amount, "IRT");

    // ---------- NewCredit factory ----------

    [Fact]
    public void NewCredit_WithValidInput_ReturnsCreditEntryWithExpectedState()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var amount = Rial(50_000m);
        var balanceAfter = Rial(150_000m);
        var referenceId = Guid.NewGuid().ToString("N");
        var idempotencyKey = "idem-key";
        var correlationId = "corr-id";

        var sut = WalletLedgerEntry.NewCredit(
            walletId, ownerId, amount, balanceAfter,
            "top-up", referenceId, idempotencyKey, correlationId);

        sut.Id.ShouldNotBeNull();
        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.Amount.ShouldBe(amount);
        sut.BalanceAfter.ShouldBe(balanceAfter);
        sut.TransactionType.ShouldBe(WalletTransactionType.Credit);
        sut.Description.ShouldBe("top-up");
        sut.ReferenceId.ShouldBe(referenceId);
        sut.IdempotencyKey.ShouldBe(idempotencyKey);
        sut.CorrelationId.ShouldBe(correlationId);
        sut.DebitRequestId.ShouldBeNull();
        sut.WithdrawalRequestId.ShouldBeNull();
        sut.TransferId.ShouldBeNull();
        sut.TopUpId.ShouldBeNull();
    }

    [Fact]
    public void NewCredit_SetsOccurredAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new WalletLedgerEntryBuilder().AsCredit().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.OccurredAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.OccurredAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void NewCredit_WithCorrelatedIds_StoresAllForeignKeys()
    {
        var debitId = WalletDebitRequestId.NewId();
        var withdrawalId = WalletWithdrawalRequestId.NewId();
        var transferId = WalletTransferId.NewId();
        var topUpId = WalletTopUpId.NewId();

        var sut = WalletLedgerEntry.NewCredit(
            WalletId.NewId(), UserId.NewId(),
            Rial(10_000m), Rial(10_000m),
            "refund", "ref", null, null,
            debitRequestId: debitId,
            withdrawalRequestId: withdrawalId,
            transferId: transferId,
            topUpId: topUpId);

        sut.DebitRequestId.ShouldBe(debitId);
        sut.WithdrawalRequestId.ShouldBe(withdrawalId);
        sut.TransferId.ShouldBe(transferId);
        sut.TopUpId.ShouldBe(topUpId);
    }

    // ---------- NewDebit factory ----------

    [Fact]
    public void NewDebit_WithValidInput_ReturnsDebitEntryWithExpectedState()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var amount = Rial(30_000m);
        var balanceAfter = Rial(70_000m);

        var sut = WalletLedgerEntry.NewDebit(
            walletId, ownerId, amount, balanceAfter,
            "purchase", "order-123", null);

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.Amount.ShouldBe(amount);
        sut.BalanceAfter.ShouldBe(balanceAfter);
        sut.TransactionType.ShouldBe(WalletTransactionType.Debit);
        sut.Description.ShouldBe("purchase");
        sut.ReferenceId.ShouldBe("order-123");
        sut.IdempotencyKey.ShouldBeNull();
    }

    // ---------- Description / IdempotencyKey / CorrelationId truncation ----------

    [Fact]
    public void NewCredit_WithDescriptionLongerThan500Chars_TruncatesTo500()
    {
        var longDescription = new string('a', 750);

        var sut = WalletLedgerEntry.NewCredit(
            WalletId.NewId(), UserId.NewId(),
            Rial(10_000m), Rial(10_000m),
            longDescription, "ref", null);

        sut.Description!.Length.ShouldBe(500);
    }

    [Fact]
    public void NewCredit_WithIdempotencyKeyLongerThan200Chars_TruncatesTo200()
    {
        var longKey = new string('k', 300);

        var sut = WalletLedgerEntry.NewCredit(
            WalletId.NewId(), UserId.NewId(),
            Rial(10_000m), Rial(10_000m),
            "d", "ref", longKey);

        sut.IdempotencyKey!.Length.ShouldBe(200);
    }

    [Fact]
    public void NewCredit_WithCorrelationIdLongerThan128Chars_TruncatesTo128()
    {
        var longCorr = new string('c', 200);

        var sut = WalletLedgerEntry.NewCredit(
            WalletId.NewId(), UserId.NewId(),
            Rial(10_000m), Rial(10_000m),
            "d", "ref", null, longCorr);

        sut.CorrelationId!.Length.ShouldBe(128);
    }

    [Fact]
    public void NewCredit_WithNullDescriptionAndOptionalFields_StoresNullValues()
    {
        var sut = WalletLedgerEntry.NewCredit(
            WalletId.NewId(), UserId.NewId(),
            Rial(10_000m), Rial(10_000m),
            description: null,
            referenceId: "ref",
            idempotencyKey: null,
            correlationId: null);

        sut.Description.ShouldBeNull();
        sut.IdempotencyKey.ShouldBeNull();
        sut.CorrelationId.ShouldBeNull();
    }

    // ---------- Validation ----------

    [Fact]
    public void NewCredit_WithNullWalletId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletLedgerEntry.NewCredit(null!, UserId.NewId(), Rial(1m), Rial(1m), "d", "ref", null));
    }

    [Fact]
    public void NewCredit_WithNullOwnerId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletLedgerEntry.NewCredit(WalletId.NewId(), null!, Rial(1m), Rial(1m), "d", "ref", null));
    }

    [Fact]
    public void NewCredit_WithNullAmount_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletLedgerEntry.NewCredit(WalletId.NewId(), UserId.NewId(), null!, Rial(1m), "d", "ref", null));
    }

    [Fact]
    public void NewCredit_WithNullBalanceAfter_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletLedgerEntry.NewCredit(WalletId.NewId(), UserId.NewId(), Rial(1m), null!, "d", "ref", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NewCredit_WithBlankReferenceId_ThrowsArgumentException(string? referenceId)
    {
        Should.Throw<ArgumentException>(() =>
            WalletLedgerEntry.NewCredit(WalletId.NewId(), UserId.NewId(), Rial(1m), Rial(1m), "d", referenceId!, null));
    }

    [Fact]
    public void NewCredit_WithReferenceIdLongerThan200Chars_ThrowsDomainException()
    {
        var longRef = new string('r', 201);

        Should.Throw<DomainException>(() =>
            WalletLedgerEntry.NewCredit(WalletId.NewId(), UserId.NewId(), Rial(1m), Rial(1m), "d", longRef, null));
    }

    [Fact]
    public void NewCredit_WithZeroAmount_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletLedgerEntry.NewCredit(WalletId.NewId(), UserId.NewId(), Rial(0m), Rial(0m), "d", "ref", null));
    }

    [Fact]
    public void NewDebit_WithZeroAmount_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletLedgerEntry.NewDebit(WalletId.NewId(), UserId.NewId(), Rial(0m), Rial(0m), "d", "ref", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NewDebit_WithBlankReferenceId_ThrowsArgumentException(string? referenceId)
    {
        Should.Throw<ArgumentException>(() =>
            WalletLedgerEntry.NewDebit(WalletId.NewId(), UserId.NewId(), Rial(1m), Rial(1m), "d", referenceId!, null));
    }

    // ---------- FromCreditEvent ----------

    [Fact]
    public void FromCreditEvent_MapsEventFieldsToNewCreditEntry()
    {
        var walletId = WalletId.NewId();
        var userId = UserId.NewId();
        var amount = Rial(25_000m);
        var newBalance = Rial(75_000m);
        var evt = new WalletCreditedEvent(
            walletId, userId, amount, newBalance,
            "credit-desc", "ref-1",
            idempotencyKey: "idem",
            correlationId: "corr",
            topUpId: WalletTopUpId.NewId());

        var sut = WalletLedgerEntry.FromCreditEvent(evt);

        sut.TransactionType.ShouldBe(WalletTransactionType.Credit);
        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
        sut.BalanceAfter.ShouldBe(newBalance);
        sut.Description.ShouldBe("credit-desc");
        sut.ReferenceId.ShouldBe("ref-1");
        sut.IdempotencyKey.ShouldBe("idem");
        sut.CorrelationId.ShouldBe("corr");
        sut.TopUpId.ShouldBe(evt.TopUpId);
    }

    [Fact]
    public void FromCreditEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => WalletLedgerEntry.FromCreditEvent(null!));
    }

    // ---------- FromDebitEvent ----------

    [Fact]
    public void FromDebitEvent_MapsEventFieldsToNewDebitEntry()
    {
        var walletId = WalletId.NewId();
        var userId = UserId.NewId();
        var amount = Rial(15_000m);
        var newBalance = Rial(35_000m);
        var evt = new WalletDebitedEvent(
            walletId, userId, amount, newBalance,
            "debit-desc", "ref-2",
            transferId: WalletTransferId.NewId());

        var sut = WalletLedgerEntry.FromDebitEvent(evt);

        sut.TransactionType.ShouldBe(WalletTransactionType.Debit);
        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
        sut.BalanceAfter.ShouldBe(newBalance);
        sut.Description.ShouldBe("debit-desc");
        sut.ReferenceId.ShouldBe("ref-2");
        sut.TransferId.ShouldBe(evt.TransferId);
    }

    [Fact]
    public void FromDebitEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => WalletLedgerEntry.FromDebitEvent(null!));
    }

    // ---------- Builder-driven parametric checks ----------

    [Fact]
    public void Builder_AsCredit_ProducesCreditTransactionType()
    {
        var sut = new WalletLedgerEntryBuilder()
            .AsCredit()
            .WithAmount(1000m)
            .WithBalanceAfter(5000m)
            .Build();

        sut.TransactionType.ShouldBe(WalletTransactionType.Credit);
    }

    [Fact]
    public void Builder_AsDebit_ProducesDebitTransactionType()
    {
        var sut = new WalletLedgerEntryBuilder()
            .AsDebit()
            .WithAmount(500m)
            .WithBalanceAfter(2500m)
            .Build();

        sut.TransactionType.ShouldBe(WalletTransactionType.Debit);
    }
}
