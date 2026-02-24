namespace Domain.Order;

/// <summary>
/// موجودیت پایا برای ثبت وضعیت Saga/Process Manager
/// این کلاس وضعیت فرآیند پردازش سفارش را در دیتابیس ذخیره می‌کند
/// تا در صورت crash یا restart، وضعیت از دست نرود
/// </summary>
public class OrderProcessState
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public string CurrentStep { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? CorrelationId { get; private set; }

    
    public static class Steps
    {
        public const string Created = "Created";
        public const string InventoryReserving = "InventoryReserving";
        public const string InventoryReserved = "InventoryReserved";
        public const string PaymentPending = "PaymentPending";
        public const string PaymentSucceeded = "PaymentSucceeded";
        public const string Completed = "Completed";
        public const string Compensating = "Compensating";
        public const string Compensated = "Compensated";
        public const string Failed = "Failed";
    }

    private OrderProcessState()
    { }

    public static OrderProcessState Create(int orderId, string? correlationId = null)
    {
        return new OrderProcessState
        {
            OrderId = orderId,
            CurrentStep = Steps.Created,
            Status = "InProgress",
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    public void TransitionTo(string step)
    {
        CurrentStep = step;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        CurrentStep = Steps.Completed;
        Status = "Completed";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        CurrentStep = Steps.Failed;
        Status = "Failed";
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensating()
    {
        CurrentStep = Steps.Compensating;
        Status = "Compensating";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensated()
    {
        CurrentStep = Steps.Compensated;
        Status = "Compensated";
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}