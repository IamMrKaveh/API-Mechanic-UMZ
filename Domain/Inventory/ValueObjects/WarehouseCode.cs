using System.Text.RegularExpressions;

namespace Domain.Inventory.ValueObjects;

public sealed class WarehouseCode : ValueObject
{
    public const int MaxLength = 20;
    private const int MinLength = 2;
    private static readonly Regex ValidPattern = new(@"^[A-Z0-9\-_]+$", RegexOptions.Compiled);

    public string Value { get; }

    private WarehouseCode(string value) => Value = value;

    public static WarehouseCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("کد انبار الزامی است.");

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
            throw new DomainException($"کد انبار باید بین {MinLength} و {MaxLength} کاراکتر باشد.");

        if (ValidPattern.IsMatch(normalized) is false)
            throw new DomainException("کد انبار فقط می‌تواند شامل حروف بزرگ، اعداد، خط تیره و زیرخط باشد.");

        return new WarehouseCode(normalized);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(WarehouseCode code) => code.Value;
}