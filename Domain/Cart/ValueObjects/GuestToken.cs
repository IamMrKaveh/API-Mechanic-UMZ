namespace Domain.Cart.ValueObjects;

public sealed class GuestToken : ValueObject
{
    public string Value { get; }

    private const int MaxLength = 256;
    private const string Prefix = "guest_";

    private GuestToken(string value)
    {
        Value = value;
    }

    /// <summary>
    /// تولید توکن جدید
    /// </summary>
    public static GuestToken Create()
    {
        var token = $"{Prefix}{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}";
        return new GuestToken(token);
    }

    /// <summary>
    /// ایجاد از رشته موجود
    /// </summary>
    public static GuestToken FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("توکن مهمان نمی‌تواند خالی باشد.");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new DomainException($"توکن مهمان نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return new GuestToken(trimmed);
    }

    /// <summary>
    /// تلاش برای ایجاد - بدون پرتاب Exception
    /// </summary>
    public static (bool Success, GuestToken? Token, string? Error) TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (false, null, "توکن مهمان نمی‌تواند خالی باشد.");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return (false, null, $"توکن مهمان نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return (true, new GuestToken(trimmed), null);
    }

    public bool IsSystemGenerated => Value.StartsWith(Prefix);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(GuestToken token) => token.Value;
}