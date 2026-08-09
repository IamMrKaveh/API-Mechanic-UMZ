using Domain.Review.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Review.ValueObjects;

public class ReviewVoteIdTests
{
    [Fact]
    public void NewId_ReturnsIdWithNonEmptyGuid()
    {
        var id = ReviewVoteId.NewId();

        id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoCalls_ReturnDifferentValues()
    {
        var a = ReviewVoteId.NewId();
        var b = ReviewVoteId.NewId();

        a.Value.ShouldNotBe(b.Value);
    }

    [Fact]
    public void From_WithValidGuid_ReturnsIdWithSameValue()
    {
        var guid = Guid.NewGuid();

        var id = ReviewVoteId.From(guid);

        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => ReviewVoteId.From(Guid.Empty));
    }

    [Fact]
    public void ImplicitOperatorGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var id = ReviewVoteId.From(guid);

        Guid primitive = id;

        primitive.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var id = ReviewVoteId.From(guid);

        id.ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void Equality_TwoIdsWithSameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = ReviewVoteId.From(guid);
        var b = ReviewVoteId.From(guid);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }
}

