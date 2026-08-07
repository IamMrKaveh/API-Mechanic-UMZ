using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wallet.ValueObjects;

public class WalletDebitRequestIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        WalletDebitRequestId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => WalletDebitRequestId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        WalletDebitRequestId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        WalletDebitRequestId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
