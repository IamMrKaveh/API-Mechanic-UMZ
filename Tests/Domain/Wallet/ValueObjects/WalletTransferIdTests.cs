using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wallet.ValueObjects;

public class WalletTransferIdTests
{
    [Fact]
    public void NewId_Always_ProducesNonEmptyGuidValue()
    {
        var sut = WalletTransferId.NewId();

        sut.ShouldNotBeNull();
        sut.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_CalledTwice_ProducesDistinctInstances()
    {
        var first = WalletTransferId.NewId();
        var second = WalletTransferId.NewId();

        first.Value.ShouldNotBe(second.Value);
        first.ShouldNotBe(second);
    }

    [Fact]
    public void NewId_ProducesInstanceAssignableToIStronglyTypedId()
    {
        WalletTransferId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void From_WithNonEmptyGuid_WrapsGuidWithoutMutation()
    {
        var guid = Guid.NewGuid();

        var sut = WalletTransferId.From(guid);

        sut.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => WalletTransferId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainExceptionWithExpectedMessage()
    {
        var exception = Should.Throw<DomainException>(() => WalletTransferId.From(Guid.Empty));

        exception.Message.ShouldBe("WalletTransferId cannot be empty.");
    }

    [Fact]
    public void From_WithSameGuidTwice_ProducesEqualRecords()
    {
        var guid = Guid.NewGuid();

        var a = WalletTransferId.From(guid);
        var b = WalletTransferId.From(guid);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void From_WithDifferentGuids_ProducesUnequalRecords()
    {
        var a = WalletTransferId.From(Guid.NewGuid());
        var b = WalletTransferId.From(Guid.NewGuid());

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public void ImplicitConversionToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var sut = WalletTransferId.From(guid);

        Guid asGuid = sut;

        asGuid.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();
        var sut = WalletTransferId.From(guid);

        sut.ToString().ShouldBe(guid.ToString());
    }
}

