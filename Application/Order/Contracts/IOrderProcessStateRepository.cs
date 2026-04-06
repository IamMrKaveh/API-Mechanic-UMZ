using Domain.Order.Enums;

namespace Application.Order.Contracts;

public interface IOrderProcessStateRepository
{
    Task<OrderProcessState?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task SaveAsync(OrderProcessState state, CancellationToken ct = default);

    Task UpdateAsync(OrderProcessState state, CancellationToken ct = default);
}

public class OrderProcessState
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public ProcessStatusEnum Status { get; set; }
    public ProcessStepEnum CurrentStep { get; set; }
    public string? LastError { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}