namespace Application.Common.Interfaces.Persistence;

public interface IInventoryTransactionRepository
{
    Task AddAsync(InventoryTransaction transaction);
    Task<(IEnumerable<InventoryTransaction> transactions, int totalCount)> GetByVariantIdAsync(int variantId, int page, int pageSize);
}