namespace Domain.Product.ValueObjects;

public sealed record ProductId : StronglyTypedId<ProductId>
{
    private ProductId(Guid value) : base(value) { }

    public static implicit operator Guid(ProductId id) => id.Value;

    public override string ToString() => Value.ToString();
}
