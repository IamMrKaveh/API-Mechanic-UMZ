namespace Domain.Discount.ValueObjects;

/// <summary>
/// Value Object برای کد تخفیف - جلوگیری از Primitive Obsession
/// تمام اعتبارسنجی در constructor انجام می‌شود
/// </summary>
public sealed class DiscountCodeValue : ValueObject
{
    private const int MinLength = 3;
    private const int MaxLength = 20;

    private static readonly System.Text.RegularExpressions.Regex ValidPattern =
        new(@"^[A-Z0-9\-_]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public string Value { get; }

    private DiscountCodeValue(string value)
    {
        Value = value;
    }

    public static DiscountCodeValue Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("کد تخفیف الزامی است.");

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length < MinLength)
            throw new DomainException($"کد تخفیف باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"کد تخفیف نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        if (!ValidPattern.IsMatch(normalized))
            throw new DomainException("کد تخفیف فقط می‌تواند شامل حروف، اعداد، خط تیره و زیرخط باشد.");

        return new DiscountCodeValue(normalized);
    }

    /// <summary>
    /// برای استفاده توسط EF Core هنگام بارگذاری از دیتابیس (بدون اعتبارسنجی)
    /// </summary>
    public static DiscountCodeValue FromPersistedString(string value)
        => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(DiscountCodeValue codeValue) => codeValue.Value;
}