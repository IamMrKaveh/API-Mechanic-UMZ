namespace Application.Common.Interfaces.Persistence;

public interface IInventoryRepository
{
    Task<Domain.Product.ProductVariant?> GetVariantByIdAsync(int variantId);

    void SetVariantRowVersion(Domain.Product.ProductVariant variant, byte[] rowVersion);

    Task AddTransactionAsync(Domain.Inventory.InventoryTransaction transaction);

    Task<(IEnumerable<Domain.Inventory.InventoryTransaction> transactions, int total)> GetTransactionsAsync(int variantId, int page, int pageSize);
}