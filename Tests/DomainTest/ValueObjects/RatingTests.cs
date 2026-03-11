namespace Tests.DomainTest.ValueObjects;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_WithValidRating_ShouldSucceed(int value)
    {
        var rating = Rating.Create(value);

        rating.Should().NotBeNull();
        rating.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithZeroRating_ShouldThrowDomainException()
    {
        var act = () => Rating.Create(0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNegativeRating_ShouldThrowDomainException()
    {
        var act = () => Rating.Create(-1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithRatingAboveFive_ShouldThrowDomainException()
    {
        var act = () => Rating.Create(6);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ImplicitConversion_ToInt_ShouldReturnValue()
    {
        var rating = Rating.Create(4);

        int value = rating;

        value.Should().Be(4);
    }

    [Fact]
    public void ImplicitConversion_FromInt_ShouldCreateRating()
    {
        Rating rating = 3;

        rating.Value.Should().Be(3);
    }

    [Fact]
    public void TwoRatings_WithSameValue_ShouldBeEqual()
    {
        var a = Rating.Create(4);
        var b = Rating.Create(4);

        a.Should().Be(b);
    }

    [Fact]
    public void TwoRatings_WithDifferentValues_ShouldNotBeEqual()
    {
        var a = Rating.Create(3);
        var b = Rating.Create(5);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentation()
    {
        var rating = Rating.Create(5);

        rating.ToString().Should().Be("5");
    }
}