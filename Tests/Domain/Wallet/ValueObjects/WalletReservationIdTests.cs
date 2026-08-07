using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wallet.ValueObjects;

public class WalletReservationIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        WalletReservationId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => WalletReservationId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        WalletReservationId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        WalletReservationId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
