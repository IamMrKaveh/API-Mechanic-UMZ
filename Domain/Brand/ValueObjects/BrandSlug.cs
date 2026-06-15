namespace Domain.Brand.ValueObjects;

public sealed class BrandSlug : Slug
{
    private BrandSlug(string value) : base(value)
    {
    }

    private BrandSlug() : base()
    {
    }

    public new static BrandSlug Create(string value) => new(NormalizeValue(value));

    public new static BrandSlug FromString(string value) => new(NormalizeFromString(value));

    public new static BrandSlug GenerateFrom(string displayName) => new(NormalizeFromDisplay(displayName));
}