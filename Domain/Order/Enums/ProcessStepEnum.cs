namespace Domain.Order.Enums;

public enum ProcessStepEnum
{
    Created,
    InventoryReserving,
    InventoryReserved,
    PaymentPending,
    PaymentSucceeded,
    Completed,
    Compensating,
    Compensated,
    Failed
}