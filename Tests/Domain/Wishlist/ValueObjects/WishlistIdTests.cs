using Domain.Wishlist.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wishlist.ValueObjects;

public class WishlistIdTests
{
    [Fact]
    public void NewId_Always_ProducesNonEmptyGuidValue()
    {
        var sut = WishlistId.NewId();

        sut.ShouldNotBeNull();
        sut.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_CalledTwice_ProducesDistinctInstances()
    {
        var first = WishlistId.NewId();
        var second = WishlistId.NewId();

        first.Value.ShouldNotBe(second.Value);
        first.ShouldNotBe(second);
    }

    [Fact]
    public void NewId_ProducesInstanceAssignableToIStronglyTypedId()
    {
        WishlistId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void From_WithNonEmptyGuid_WrapsGuidWithoutMutation()
    {
        var guid = Guid.NewGuid();

        var sut = WishlistId.From(guid);

        sut.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => WishlistId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithSameGuidTwice_ProducesEqualRecords()
    {
        var guid = Guid.NewGuid();

        var a = WishlistId.From(guid);
        var b = WishlistId.From(guid);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void From_WithDifferentGuids_ProducesUnequalRecords()
    {
        var a = WishlistId.From(Guid.NewGuid());
        var b = WishlistId.From(Guid.NewGuid());

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public void ImplicitConversionToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var sut = WishlistId.From(guid);

        Guid asGuid = sut;

        asGuid.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();
        var sut = WishlistId.From(guid);

        sut.ToString().ShouldBe(guid.ToString());
    }
}
