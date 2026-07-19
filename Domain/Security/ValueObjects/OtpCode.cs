namespace Domain.Security.ValueObjects;

public sealed class OtpCode : ValueObject
{
    public const int Length = 6;

    public string Value { get; }

    private OtpCode(string value) => Value = value;

    public static OtpCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("کد OTP الزامی است.");

        var trimmed = value.Trim();

        if (trimmed.Length != Length)
            throw new DomainException($"کد OTP باید دقیقاً {Length} رقم باشد.");

        if (!trimmed.All(char.IsDigit))
            throw new DomainException("کد OTP باید فقط شامل اعداد باشد.");

        return new OtpCode(trimmed);
    }

    public static OtpCode Generate(int length = Length)
    {
        var digits = Enumerable.Range(0, 10).ToList();

        using var rng = RandomNumberGenerator.Create();
        for (int i = digits.Count - 1; i > 0; i--)
        {
            var box = new byte[4];
            rng.GetBytes(box);
            int j = BitConverter.ToInt32(box, 0) & int.MaxValue % (i + 1);

            (digits[i], digits[j]) = (digits[j], digits[i]);
        }

        var code = string.Concat(digits.Take(length));

        return new OtpCode(code);
    }

    public bool Matches(string providedCode)
    {
        if (string.IsNullOrWhiteSpace(providedCode))
            return false;

        var providedBytes = System.Text.Encoding.UTF8.GetBytes(providedCode.Trim());
        var actualBytes = System.Text.Encoding.UTF8.GetBytes(Value);

        if (providedBytes.Length != actualBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(providedBytes, actualBytes);
    }

    public string ToHash()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(Value);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    public bool MatchesHash(string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        var computed = ToHash();

        var computedBytes = System.Text.Encoding.UTF8.GetBytes(computed);
        var storedBytes = System.Text.Encoding.UTF8.GetBytes(storedHash);

        if (computedBytes.Length != storedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
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
