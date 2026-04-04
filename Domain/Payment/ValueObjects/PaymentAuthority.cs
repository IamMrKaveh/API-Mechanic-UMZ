using SharedKernel.Results;

namespace Domain.Payment.ValueObjects;

public sealed class PaymentAuthority : ValueObject
{
    public string Value { get; }

    private const int MaxLength = 100;
    private const int MinLength = 10;

    private PaymentAuthority(string value)
    {
        Value = value;
    }

    public static PaymentAuthority Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("شناسه پرداخت الزامی است.");

        var normalized = value.Trim();

        if (normalized.Length < MinLength)
            throw new DomainException($"شناسه پرداخت باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"شناسه پرداخت نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return new PaymentAuthority(normalized);
    }

    public static Result<PaymentAuthority> TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PaymentAuthority>.Failure(new Error(
                "400",
                "شناسه پرداخت الزامی است.",
                ErrorType.Validation));

        var normalized = value.Trim();

        if (normalized.Length < MinLength)
            return Result<PaymentAuthority>.Failure(new Error(
                "400",
                $"شناسه پرداخت باید حداقل {MinLength} کاراکتر باشد.",
                ErrorType.Validation));

        if (normalized.Length > MaxLength)
            return Result<PaymentAuthority>.Failure(new Error(
                "400",
                $"شناسه پرداخت نمی‌تواند بیش از {MaxLength} کاراکتر باشد.",
                ErrorType.Validation));

        return Result<PaymentAuthority>.Success(new PaymentAuthority(normalized));
    }

    public bool Matches(string other) => !string.IsNullOrWhiteSpace(other) && Value.Equals(other.Trim(), StringComparison.Ordinal);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PaymentAuthority authority) => authority.Value;
}