namespace Domain.Category.ValueObjects;

public sealed record CategoryId : StronglyTypedId<CategoryId>
{
    private CategoryId(Guid value) : base(value) { }

    public static implicit operator Guid(CategoryId id) => id.Value;

    public override string ToString() => Value.ToString();
}
