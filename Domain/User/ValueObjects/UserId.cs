namespace Domain.User.ValueObjects;

public sealed class UserId : ValueObject
{
    public int Value { get; }

    private UserId(int value) => Value = value;

    public static UserId Create(int value)
    {
        if (value <= 0)
            throw new DomainException("شناسه کاربر باید عدد مثبت باشد.");
        return new UserId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(UserId userId) => userId.Value;
}