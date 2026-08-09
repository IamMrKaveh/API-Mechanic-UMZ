using Domain.Review.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Review.ValueObjects;

public class ReviewIdTests
{
    [Fact]
    public void NewId_ReturnsIdWithNonEmptyGuid()
    {
        var id = ReviewId.NewId();

        id.ShouldNotBeNull();
        id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoCalls_ReturnDifferentValues()
    {
        var a = ReviewId.NewId();
        var b = ReviewId.NewId();

        a.Value.ShouldNotBe(b.Value);
    }

    [Fact]
    public void From_WithValidGuid_ReturnsIdWithSameValue()
    {
        var guid = Guid.NewGuid();

        var id = ReviewId.From(guid);

        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => ReviewId.From(Guid.Empty));
    }

    [Fact]
    public void ImplicitOperatorGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var id = ReviewId.From(guid);

        Guid primitive = id;

        primitive.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var id = ReviewId.From(guid);

        id.ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void Equality_TwoIdsWithSameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = ReviewId.From(guid);
        var b = ReviewId.From(guid);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_TwoIdsWithDifferentGuid_AreNotEqual()
    {
        var a = ReviewId.NewId();
        var b = ReviewId.NewId();

        a.ShouldNotBe(b);
    }
}

