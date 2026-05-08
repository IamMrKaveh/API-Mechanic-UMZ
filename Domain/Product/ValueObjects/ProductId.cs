namespace Domain.Product.ValueObjects;

public sealed record ProductId : IStronglyTypedId
{
    public Guid Value { get; }

    private ProductId(Guid value) => Value = value;

    public static ProductId NewId() => new(Guid.NewGuid());

    public static ProductId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("ProductId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ProductId id) => id.Value;
}