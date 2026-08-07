using Domain.Payment.Exceptions;
using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Payment.Exceptions;

public class PaymentExceptionsTests
{
    [Fact]
    public void InvalidPaymentAmountException_ExposesExpectedAndActualAndDifferenceAndErrorCode()
    {
        var sut = new InvalidPaymentAmountException(expectedAmount: 100_000m, actualAmount: 90_000m);

        sut.ExpectedAmount.ShouldBe(100_000m);
        sut.ActualAmount.ShouldBe(90_000m);
        sut.Difference.ShouldBe(10_000m);
        sut.ErrorCode.ShouldBe("INVALID_PAYMENT_AMOUNT");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidPaymentAmountException_DifferenceIsAlwaysNonNegative()
    {
        new InvalidPaymentAmountException(50m, 200m).Difference.ShouldBe(150m);
    }

    [Fact]
    public void PaymentAlreadyVerifiedException_ExposesTransactionIdAndRefIdAndErrorCode()
    {
        var transactionId = Guid.NewGuid();

        var sut = new PaymentAlreadyVerifiedException(transactionId, refId: 12345);

        sut.TransactionId.ShouldBe(transactionId);
        sut.RefId.ShouldBe(12345L);
        sut.VerifiedAt.ShouldBeNull();
        sut.ErrorCode.ShouldBe("PAYMENT_ALREADY_VERIFIED");
        sut.Message.ShouldContain(transactionId.ToString());
        sut.Message.ShouldContain("12345");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void PaymentAlreadyVerifiedException_WithVerifiedAt_StoresValue()
    {
        var verifiedAt = new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

        var sut = new PaymentAlreadyVerifiedException(Guid.NewGuid(), 1, verifiedAt);

        sut.VerifiedAt.ShouldBe(verifiedAt);
    }

    [Fact]
    public void PaymentExpiredException_ExposesAuthorityAndExpiryDateAndErrorCode()
    {
        var authority = PaymentAuthority.Create("AUTH-12345");
        var expiryDate = DateTime.UtcNow.AddMinutes(-30);

        var sut = new PaymentExpiredException(authority, expiryDate);

        sut.Authority.ShouldBe(authority);
        sut.ExpiryDate.ShouldBe(expiryDate);
        sut.ExpiredSince.ShouldBeGreaterThan(TimeSpan.Zero);
        sut.ErrorCode.ShouldBe("PAYMENT_EXPIRED");
        sut.Message.ShouldContain("AUTH-12345");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void PaymentNotVerifiableException_Parameterless_HasDefaultMessageAndErrorCode()
    {
        var sut = new PaymentNotVerifiableException();

        sut.Authority.ShouldBeNull();
        sut.ErrorCode.ShouldBe("PAYMENT_NOT_VERIFIABLE");
        sut.Message.ShouldNotBeNullOrWhiteSpace();
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void PaymentNotVerifiableException_WithAuthority_StoresAuthorityAndReferencesItInMessage()
    {
        var authority = PaymentAuthority.Create("AUTH-99999");

        var sut = new PaymentNotVerifiableException(authority);

        sut.Authority.ShouldBe(authority);
        sut.Message.ShouldContain("AUTH-99999");
        sut.ErrorCode.ShouldBe("PAYMENT_NOT_VERIFIABLE");
    }

    [Fact]
    public void PaymentTransactionNotFoundException_Parameterless_HasNullAuthorityAndErrorCode()
    {
        var sut = new PaymentTransactionNotFoundException();

        sut.Authority.ShouldBeNull();
        sut.ErrorCode.ShouldBe("PAYMENT_TRANSACTION_NOT_FOUND");
        sut.Message.ShouldNotBeNullOrWhiteSpace();
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void PaymentTransactionNotFoundException_WithAuthority_StoresAndReferencesInMessage()
    {
        var sut = new PaymentTransactionNotFoundException("AUTH-12345");

        sut.Authority.ShouldBe("AUTH-12345");
        sut.Message.ShouldContain("AUTH-12345");
        sut.ErrorCode.ShouldBe("PAYMENT_TRANSACTION_NOT_FOUND");
    }
}
