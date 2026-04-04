using SharedKernel.Results;

namespace Domain.Payment.ValueObjects;

public sealed class PaymentStatus : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public int Order { get; }
    public bool IsFinal { get; }

    private PaymentStatus(string value, string displayName, int order, bool isFinal)
    {
        Value = value;
        DisplayName = displayName;
        Order = order;
        IsFinal = isFinal;
    }

    public static PaymentStatus Pending => new("Pending", "در انتظار پرداخت", 0, false);
    public static PaymentStatus Processing => new("Processing", "در حال پردازش", 1, false);
    public static PaymentStatus Success => new("Success", "موفق", 2, true);
    public static PaymentStatus Failed => new("Failed", "ناموفق", 3, true);
    public static PaymentStatus Expired => new("Expired", "منقضی شده", 4, true);
    public static PaymentStatus Cancelled => new("Cancelled", "لغو شده", 5, true);
    public static PaymentStatus Refunded => new("Refunded", "بازگشت داده شده", 6, true);

    public static PaymentStatus FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Pending;

        return value.ToLowerInvariant() switch
        {
            "pending" => Pending,
            "processing" => Processing,
            "success" => Success,
            "failed" => Failed,
            "expired" => Expired,
            "cancelled" => Cancelled,
            "refunded" => Refunded,
            _ => throw new DomainException($"وضعیت پرداخت '{value}' نامعتبر است.")
        };
    }

    public static PaymentStatus FromInt(int value)
    {
        return value switch
        {
            0 => Pending,
            1 => Processing,
            2 => Success,
            3 => Failed,
            4 => Expired,
            5 => Cancelled,
            6 => Refunded,
            _ => throw new DomainException($"وضعیت پرداخت با کد {value} نامعتبر است.")
        };
    }

    public static Result<PaymentStatus> TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PaymentStatus>.Success(Pending);

        return value.ToLowerInvariant() switch
        {
            "pending" => Result<PaymentStatus>.Success(Pending),
            "processing" => Result<PaymentStatus>.Success(Processing),
            "success" => Result<PaymentStatus>.Success(Success),
            "failed" => Result<PaymentStatus>.Success(Failed),
            "expired" => Result<PaymentStatus>.Success(Expired),
            "cancelled" => Result<PaymentStatus>.Success(Cancelled),
            "refunded" => Result<PaymentStatus>.Success(Refunded),
            _ => Result<PaymentStatus>.Failure(new Error(
                "400",
                $"وضعیت پرداخت '{value}' نامعتبر است.",
                ErrorType.Validation))
        };
    }

    public static IEnumerable<PaymentStatus> GetAll()
    {
        yield return Pending;
        yield return Processing;
        yield return Success;
        yield return Failed;
        yield return Expired;
        yield return Cancelled;
        yield return Refunded;
    }

    public static IEnumerable<PaymentStatus> GetFinalStatuses()
    {
        return GetAll().Where(s => s.IsFinal);
    }

    public static IEnumerable<PaymentStatus> GetActiveStatuses()
    {
        return GetAll().Where(s => !s.IsFinal);
    }

    public bool IsSuccess() => Value == Success.Value;

    public bool IsPending() => Value == Pending.Value;

    public bool IsProcessing() => Value == Processing.Value;

    public bool IsFailed() => Value == Failed.Value;

    public bool IsExpired() => Value == Expired.Value;

    public bool IsCancelled() => Value == Cancelled.Value;

    public bool IsRefunded() => Value == Refunded.Value;

    public bool CanTransitionTo(PaymentStatus newStatus)
    {
        if (IsFinal && newStatus.Value != Refunded.Value)
            return false;

        return Value switch
        {
            "Pending" => newStatus == Processing || newStatus == Failed || newStatus == Expired || newStatus == Cancelled,
            "Processing" => newStatus == Success || newStatus == Failed || newStatus == Expired,
            "Success" => newStatus == Refunded,
            _ => false
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(PaymentStatus status) => status.Value;

    public static implicit operator int(PaymentStatus status) => status.Order;
}