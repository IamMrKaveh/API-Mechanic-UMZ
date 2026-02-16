namespace Domain.Review.ValueObjects;

public sealed class Rating : ValueObject, IComparable<Rating>
{
    public int Value { get; }

    private const int MinRating = 1;
    private const int MaxRating = 5;

    private Rating(int value)
    {
        Value = value;
    }

    public static Rating Create(int value)
    {
        if (value < MinRating || value > MaxRating)
            throw new DomainException($"امتیاز باید بین {MinRating} و {MaxRating} باشد.");

        return new Rating(value);
    }

    public static Rating One => new(1);
    public static Rating Two => new(2);
    public static Rating Three => new(3);
    public static Rating Four => new(4);
    public static Rating Five => new(5);

    public bool IsExcellent() => Value >= 4;

    public bool IsGood() => Value >= 3;

    public bool IsPoor() => Value <= 2;

    public int CompareTo(Rating? other)
    {
        if (other == null) return 1;
        return Value.CompareTo(other.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value} از {MaxRating}";

    public static implicit operator int(Rating rating) => rating.Value;

    public static bool operator >(Rating left, Rating right) => left.Value > right.Value;

    public static bool operator <(Rating left, Rating right) => left.Value < right.Value;

    public static bool operator >=(Rating left, Rating right) => left.Value >= right.Value;

    public static bool operator <=(Rating left, Rating right) => left.Value <= right.Value;
}