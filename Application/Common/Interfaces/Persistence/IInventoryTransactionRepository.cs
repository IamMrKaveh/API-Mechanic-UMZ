namespace Application.Common.Interfaces.Persistence;

public interface IInventoryTransactionRepository
{
    Task AddAsync(InventoryTransaction transaction);
    Task<(IEnumerable<InventoryTransaction> transactions, int totalCount)> GetByVariantIdAsync(int variantId, int page, int pageSize);
    Task<IEnumerable<InventoryTransaction>> GetByReferenceNumberAsync(string referenceNumber);
    Task<int> GetCurrentStockAsync(int variantId);
}