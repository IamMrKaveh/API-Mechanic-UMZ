using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Payment.ValueObjects;

public class PaymentMethodIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        PaymentMethodId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => PaymentMethodId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        PaymentMethodId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = PaymentMethodId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        PaymentMethodId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        PaymentMethodId.From(guid).ShouldBe(PaymentMethodId.From(guid));
    }
}
