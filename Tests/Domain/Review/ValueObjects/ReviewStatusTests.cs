using Domain.Review.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Review.ValueObjects;

public class ReviewStatusTests
{
    [Fact]
    public void StaticInstances_HaveExpectedValues()
    {
        ReviewStatus.Pending.Value.ShouldBe("Pending");
        ReviewStatus.Approved.Value.ShouldBe("Approved");
        ReviewStatus.Rejected.Value.ShouldBe("Rejected");
    }

    [Fact]
    public void StaticInstances_HaveExpectedDisplayNames()
    {
        ReviewStatus.Pending.DisplayName.ShouldBe("در انتظار تأیید");
        ReviewStatus.Approved.DisplayName.ShouldBe("تأیید شده");
        ReviewStatus.Rejected.DisplayName.ShouldBe("رد شده");
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    public void From_WithKnownValue_ReturnsCanonicalInstance(string value)
    {
        var status = ReviewStatus.From(value);

        status.Value.ShouldBe(value);
    }

    [Fact]
    public void From_Pending_ReturnsSameReferenceAsStaticPending()
    {
        var status = ReviewStatus.From("Pending");

        ReferenceEquals(status, ReviewStatus.Pending).ShouldBeTrue();
    }

    [Fact]
    public void From_Approved_ReturnsSameReferenceAsStaticApproved()
    {
        var status = ReviewStatus.From("Approved");

        ReferenceEquals(status, ReviewStatus.Approved).ShouldBeTrue();
    }

    [Fact]
    public void From_Rejected_ReturnsSameReferenceAsStaticRejected()
    {
        var status = ReviewStatus.From("Rejected");

        ReferenceEquals(status, ReviewStatus.Rejected).ShouldBeTrue();
    }

    [Fact]
    public void From_TrimsWhitespaceBeforeMatching()
    {
        var status = ReviewStatus.From("  Approved  ");

        ReferenceEquals(status, ReviewStatus.Approved).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhitespace_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() => ReviewStatus.From(value!));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("APPROVED")]
    [InlineData("Removed")]
    [InlineData("Xyz")]
    public void From_WithUnknownValue_ThrowsDomainException(string value)
    {
        Should.Throw<DomainException>(() => ReviewStatus.From(value));
    }

    [Fact]
    public void Equality_UsesValueComponent()
    {
        var a = ReviewStatus.From("Approved");
        var b = ReviewStatus.From("Approved");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        ReviewStatus.Pending.ShouldNotBe(ReviewStatus.Approved);
        (ReviewStatus.Pending == ReviewStatus.Approved).ShouldBeFalse();
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValueNotDisplayName()
    {
        string s = ReviewStatus.Approved;

        s.ShouldBe("Approved");
    }

    [Fact]
    public void ToString_ReturnsDisplayName()
    {
        ReviewStatus.Pending.ToString().ShouldBe("در انتظار تأیید");
        ReviewStatus.Approved.ToString().ShouldBe("تأیید شده");
        ReviewStatus.Rejected.ToString().ShouldBe("رد شده");
    }
}

