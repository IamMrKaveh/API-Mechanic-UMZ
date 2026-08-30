using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wallet.ValueObjects;

public class WalletFraudAlertIdTests
{
    [Fact]
    public void NewId_Always_ProducesNonEmptyGuidValue()
    {
        var sut = WalletFraudAlertId.NewId();

        sut.ShouldNotBeNull();
        sut.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_CalledTwice_ProducesDistinctInstances()
    {
        var first = WalletFraudAlertId.NewId();
        var second = WalletFraudAlertId.NewId();

        first.Value.ShouldNotBe(second.Value);
        first.ShouldNotBe(second);
    }

    [Fact]
    public void NewId_ProducesInstanceAssignableToIStronglyTypedId()
    {
        WalletFraudAlertId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void From_WithNonEmptyGuid_WrapsGuidWithoutMutation()
    {
        var guid = Guid.NewGuid();

        var sut = WalletFraudAlertId.From(guid);

        sut.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => WalletFraudAlertId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainExceptionWithExpectedMessage()
    {
        var exception = Should.Throw<DomainException>(() => WalletFraudAlertId.From(Guid.Empty));

        exception.Message.ShouldBe("WalletFraudAlertId cannot be empty.");
    }

    [Fact]
    public void From_WithSameGuidTwice_ProducesEqualRecords()
    {
        var guid = Guid.NewGuid();

        var a = WalletFraudAlertId.From(guid);
        var b = WalletFraudAlertId.From(guid);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void From_WithDifferentGuids_ProducesUnequalRecords()
    {
        var a = WalletFraudAlertId.From(Guid.NewGuid());
        var b = WalletFraudAlertId.From(Guid.NewGuid());

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public void ImplicitConversionToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var sut = WalletFraudAlertId.From(guid);

        Guid asGuid = sut;

        asGuid.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();
        var sut = WalletFraudAlertId.From(guid);

        sut.ToString().ShouldBe(guid.ToString());
    }
}

