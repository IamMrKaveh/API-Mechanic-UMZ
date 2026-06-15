namespace Domain.Product.ValueObjects;

public sealed class ProductSlug : Slug
{
    private ProductSlug(string value) : base(value)
    {
    }

    private ProductSlug() : base()
    {
    }

    public new static ProductSlug Create(string value) => new(NormalizeValue(value));

    public new static ProductSlug FromString(string value) => new(NormalizeFromString(value));

    public new static ProductSlug GenerateFrom(string displayName) => new(NormalizeFromDisplay(displayName));
}