namespace Domain.Security.ValueObjects;

public sealed class OtpCode : ValueObject
{
    public string Value { get; }

    private const int DefaultLength = 6;
    private const int MinLength = 4;
    private const int MaxLength = 8;

    private OtpCode(string value) => Value = value;

    public static OtpCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("کد OTP الزامی است.");

        var normalized = value.Trim();

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
            throw new DomainException($"کد OTP باید بین {MinLength} تا {MaxLength} کاراکتر باشد.");

        if (!normalized.All(char.IsDigit))
            throw new DomainException("کد OTP فقط باید شامل اعداد باشد.");

        return new OtpCode(normalized);
    }

    public static OtpCode Generate(int length = DefaultLength)
    {
        if (length < MinLength || length > MaxLength)
            throw new DomainException($"طول کد OTP باید بین {MinLength} تا {MaxLength} باشد.");

        var random = new Random();
        var code = string.Concat(Enumerable.Range(0, length).Select(_ => random.Next(0, 10)));
        return new OtpCode(code);
    }

    public bool Matches(string providedCode)
    {
        if (string.IsNullOrWhiteSpace(providedCode))
            return false;

        return string.Equals(Value, providedCode.Trim(), StringComparison.Ordinal);
    }

    public string GetMasked()
    {
        if (Value.Length <= 2)
            return new string('*', Value.Length);

        return $"{Value[0]}{new string('*', Value.Length - 2)}{Value[^1]}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OtpCode code) => code.Value;
}