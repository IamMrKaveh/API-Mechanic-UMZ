namespace Domain.Review.ValueObjects;

public sealed record Rating
{
    public int Value { get; }

    private Rating(int value) => Value = value;

    public static Rating Create(int value)
    {
        if (value < 1 || value > 5)
            throw new DomainException("امتیاز باید بین ۱ تا ۵ باشد.");

        return new Rating(value);
    }

    public static bool TryCreate(int value, out Rating? rating)
    {
        if (value < 1 || value > 5)
        {
            rating = null;
            return false;
        }

        rating = new Rating(value);
        return true;
    }

    public static implicit operator int(Rating rating) => rating.Value;

    public static explicit operator Rating(int value) => Create(value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
