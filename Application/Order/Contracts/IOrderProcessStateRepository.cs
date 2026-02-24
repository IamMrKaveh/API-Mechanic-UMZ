namespace Application.Order.Contracts;

/// <summary>
/// Repository interface برای ذخیره وضعیت Saga در دیتابیس
/// </summary>
public interface IOrderProcessStateRepository
{
    Task<OrderProcessState?> GetByOrderIdAsync(
        int orderId,
        CancellationToken ct = default
        );

    Task AddAsync(
        OrderProcessState state,
        CancellationToken ct = default
        );

    void Update(
        OrderProcessState state
        );
}