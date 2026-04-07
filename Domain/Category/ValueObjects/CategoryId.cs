namespace Domain.Category.ValueObjects;

public sealed record CategoryId
{
    public Guid Value { get; }

    private CategoryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(value));

        Value = value;
    }

    public static CategoryId NewId() => new(Guid.NewGuid());

    public static CategoryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}