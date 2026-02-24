namespace Domain.Inventory.ValueObjects;

public sealed class WarehouseCode : ValueObject
{
    public string Value { get; }

    private const int MaxLength = 50;
    private const int MinLength = 2;

    private WarehouseCode(string value) => Value = value;

    public static WarehouseCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("کد انبار الزامی است.");

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length < MinLength)
            throw new DomainException($"کد انبار باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"کد انبار نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z0-9\-_]+$"))
            throw new DomainException("کد انبار فقط می‌تواند شامل حروف انگلیسی، اعداد، خط تیره و زیرخط باشد.");

        return new WarehouseCode(normalized);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(WarehouseCode code) => code.Value;
}