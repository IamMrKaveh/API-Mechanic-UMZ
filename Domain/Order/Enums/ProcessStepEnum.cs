namespace Domain.Order.Enums;

public enum ProcessStepEnum
{
    Created,
    InventoryReserving,
    InventoryReserved,
    PaymentPending,
    PaymentSucceeded,
    InventoryCommitting,
    InventoryCommitFailed,
    Refunded,
    RequiresManualReconciliation,
    Completed,
    Compensating,
    Compensated,
    Failed
}
