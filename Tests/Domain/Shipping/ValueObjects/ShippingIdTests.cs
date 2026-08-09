using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Shipping.ValueObjects;

public class ShippingIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        var sut = ShippingId.NewId();

        sut.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocationsProduceDifferentValues()
    {
        ShippingId.NewId().Value.ShouldNotBe(ShippingId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsInstanceWithSameValue()
    {
        var guid = Guid.NewGuid();

        var sut = ShippingId.From(guid);

        sut.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => ShippingId.From(Guid.Empty))
            .Message.ShouldContain("ShippingId");
    }

    [Fact]
    public void Equality_ForRecordWithSameGuid_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        ShippingId.From(guid).ShouldBe(ShippingId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentGuid_TreatsInstancesAsNotEqual()
    {
        ShippingId.From(Guid.NewGuid()).ShouldNotBe(ShippingId.From(Guid.NewGuid()));
    }

    [Fact]
    public void ImplicitOperatorGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var sut = ShippingId.From(guid);

        Guid extracted = sut;

        extracted.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();

        ShippingId.From(guid).ToString().ShouldBe(guid.ToString());
    }
}

