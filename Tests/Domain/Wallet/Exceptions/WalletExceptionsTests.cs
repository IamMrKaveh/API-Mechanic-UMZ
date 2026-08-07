using Domain.Wallet.Enums;
using Domain.Wallet.Exceptions;
using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Localization;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wallet.Exceptions;

public class WalletExceptionsTests
{
    [Fact]
    public void InsufficientWalletBalanceException_ExposesAllArgumentsAndErrorCode()
    {
        var walletId = WalletId.NewId();
        var requested = Money.Create(100m, "IRT");
        var available = Money.Create(30m, "IRT");

        var sut = new InsufficientWalletBalanceException(walletId, requested, available);

        sut.Args["walletId"].ShouldBe(walletId.Value);
        sut.Args["requestedAmount"].ShouldBe(requested.Amount);
        sut.Args["availableAmount"].ShouldBe(available.Amount);
        sut.ErrorCode.ShouldBe(DomainErrorCodes.Wallet.InsufficientBalance);
        sut.Message.ShouldContain(walletId.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void WalletInactiveException_ExposesWalletIdAndErrorCode()
    {
        var walletId = WalletId.NewId();

        var sut = new WalletInactiveException(walletId);

        sut.Args["walletId"].ShouldBe(walletId.Value);
        sut.ErrorCode.ShouldBe(DomainErrorCodes.Wallet.Inactive);
        sut.Message.ShouldContain(walletId.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidWalletAmountException_ExposesAmountAndErrorCode()
    {
        var sut = new InvalidWalletAmountException(-50m);

        sut.Args["amount"].ShouldBe(-50m);
        sut.ErrorCode.ShouldBe(DomainErrorCodes.Wallet.InvalidAmount);
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void WalletDebitRequestNotFoundException_InheritsFromDomainExceptionAndCarriesMessage()
    {
        var requestId = WalletDebitRequestId.NewId();

        var sut = new WalletDebitRequestNotFoundException(requestId);

        sut.Message.ShouldContain(requestId.Value.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void WalletDebitRequestExpiredException_InheritsFromDomainException()
    {
        var sut = new WalletDebitRequestExpiredException();

        sut.Message.ShouldNotBeNullOrWhiteSpace();
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void UnauthorizedWalletDebitApprovalException_InheritsFromDomainException()
    {
        var sut = new UnauthorizedWalletDebitApprovalException();

        sut.Message.ShouldNotBeNullOrWhiteSpace();
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidWalletDebitRequestStatusException_InheritsFromDomainExceptionAndMessageIncludesStatus()
    {
        var sut = new InvalidWalletDebitRequestStatusException(WalletDebitRequestStatus.Approved.ToString());

        sut.Message.ShouldContain("Approved");
        sut.ShouldBeAssignableTo<DomainException>();
    }
}
