namespace Application.Common.Interfaces;

public interface IInventoryService
{
    Task LogTransactionAsync(int variantId, string transactionType, int quantityChange, int? orderItemId, int? userId, string? notes, string? referenceNumber = null, byte[]? rowVersion = null);

    Task<(IEnumerable<Domain.Inventory.InventoryTransaction> transactions, int total)> GetTransactionsAsync(int variantId, int page, int pageSize);

    Task<int> GetCurrentStockAsync(int variantId);

    Task<(bool success, string message)> AdjustStockAsync(int variantId, int newStock, int userId, string notes);
}