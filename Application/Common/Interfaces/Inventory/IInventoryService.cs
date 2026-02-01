namespace Application.Common.Interfaces.Inventory;

public interface IInventoryService
{
    Task LogTransactionAsync(
        int variantId,
        string transactionType,
        int quantityChange,
        int? orderItemId,
        int? userId,
        string notes,
        string? referenceNumber,
        byte[]? rowVersion = null,
        bool saveChanges = true);

    Task RollbackReservationsAsync(string referenceNumber);

    Task<bool> ReconcileStockAsync(int variantId);
}