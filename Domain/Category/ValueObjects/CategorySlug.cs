namespace Domain.Category.ValueObjects;

public sealed class CategorySlug : Slug
{
    private CategorySlug(string value) : base(value)
    {
    }

    private CategorySlug() : base()
    {
    }

    public new static CategorySlug Create(string value) => new(NormalizeValue(value));

    public new static CategorySlug FromString(string value) => new(NormalizeFromString(value));

    public new static CategorySlug GenerateFrom(string displayName) => new(NormalizeFromDisplay(displayName));
}