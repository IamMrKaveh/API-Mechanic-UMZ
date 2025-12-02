namespace Application.Common.Interfaces;

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
}