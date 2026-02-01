namespace Infrastructure.Persistence.Interface.Inventory;

public interface IInventoryRepository
{
    Task<ProductVariant?> GetVariantByIdAsync(int variantId);

    void SetVariantRowVersion(ProductVariant variant, byte[] rowVersion);

    Task AddTransactionAsync(InventoryTransaction transaction);

    Task<(IEnumerable<InventoryTransaction> transactions, int total)> GetTransactionsAsync(int variantId, int page, int pageSize);
}