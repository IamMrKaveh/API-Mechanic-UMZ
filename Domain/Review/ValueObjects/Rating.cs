namespace Domain.Review.ValueObjects;

public sealed class Rating : ValueObject
{
    public int Value { get; }

    private Rating(int value)
    {
        Value = value;
    }

    public static Rating Create(int value)
    {
        if (value < 1 || value > 5)
            throw new DomainException("امتیاز باید بین ۱ تا ۵ باشد.");

        return new Rating(value);
    }

    public static implicit operator int(Rating rating) => rating.Value;

    public static implicit operator Rating(int value) => Create(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}