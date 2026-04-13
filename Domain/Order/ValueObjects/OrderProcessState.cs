using Domain.Order.Enums;

namespace Domain.Order.ValueObjects;

public class OrderProcessState
{
    public Guid Id { get; private set; }
    public OrderId OrderId { get; private set; }
    public ProcessStepEnum CurrentStep { get; private set; }
    public ProcessStatusEnum Status { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? CorrelationId { get; private set; }

    private OrderProcessState()
    { }

    public static OrderProcessState Create(OrderId orderId, string? correlationId = null)
    {
        return new OrderProcessState
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CurrentStep = ProcessStepEnum.Created,
            Status = ProcessStatusEnum.InProgress,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    public void TransitionTo(ProcessStepEnum step)
    {
        CurrentStep = step;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        CurrentStep = ProcessStepEnum.Completed;
        Status = ProcessStatusEnum.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        CurrentStep = ProcessStepEnum.Failed;
        Status = ProcessStatusEnum.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensating()
    {
        CurrentStep = ProcessStepEnum.Compensating;
        Status = ProcessStatusEnum.Compensating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensated()
    {
        CurrentStep = ProcessStepEnum.Compensated;
        Status = ProcessStatusEnum.Compensated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}