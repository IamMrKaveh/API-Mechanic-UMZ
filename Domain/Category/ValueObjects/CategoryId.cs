namespace Domain.Category.ValueObjects;

public sealed record CategoryId : IStronglyTypedId
{
    public Guid Value { get; }

    private CategoryId(Guid value) => Value = value;

    public static CategoryId NewId() => new(Guid.NewGuid());

    public static CategoryId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("CategoryId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CategoryId id) => id.Value;
}