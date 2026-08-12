using Domain.Review.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Review.ValueObjects;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_WithValueInRange_ReturnsRatingWithSameValue(int value)
    {
        var rating = Rating.Create(value);

        rating.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Create_WithValueOutOfRange_ThrowsDomainException(int value)
    {
        Should.Throw<DomainException>(() => Rating.Create(value));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void TryCreate_ReturnsExpectedFlagAndValue(int value, bool expected)
    {
        var success = Rating.TryCreate(value, out var rating);

        success.ShouldBe(expected);
        if (expected)
        {
            rating.ShouldNotBeNull();
            rating!.Value.ShouldBe(value);
        }
        else
        {
            rating.ShouldBeNull();
        }
    }

    [Fact]
    public void ImplicitOperatorInt_ReturnsUnderlyingValue()
    {
        var rating = Rating.Create(3);

        int primitive = rating;

        primitive.ShouldBe(3);
    }

    [Fact]
    public void ExplicitOperatorRating_WithValidValue_ReturnsRating()
    {
        var rating = (Rating)4;

        rating.Value.ShouldBe(4);
    }

    [Fact]
    public void ExplicitOperatorRating_WithInvalidValue_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => (Rating)0);
    }

    [Fact]
    public void ToString_ReturnsInvariantValue()
    {
        var rating = Rating.Create(5);

        rating.ToString().ShouldBe("5");
    }

    [Fact]
    public void Equality_TwoRatingsWithSameValue_AreEqual()
    {
        var a = Rating.Create(4);
        var b = Rating.Create(4);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_TwoRatingsWithDifferentValue_AreNotEqual()
    {
        var a = Rating.Create(3);
        var b = Rating.Create(4);

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }
}
