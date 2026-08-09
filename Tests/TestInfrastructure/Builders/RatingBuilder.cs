using Domain.Review.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class RatingBuilder
{
    private int _value = 5;

    public RatingBuilder WithValue(int value)
    {
        _value = value;
        return this;
    }

    public Rating Build() => Rating.Create(_value);
}
