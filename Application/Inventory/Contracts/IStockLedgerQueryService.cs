namespace Application.Inventory.Contracts;

public interface IStockLedgerQueryService
{
    Task<int> GetCurrentBalanceAsync(int variantId, int? warehouseId = null, CancellationToken ct = default);

    Task<IEnumerable<StockLedgerEntry>> GetLedgerAsync(
        int variantId, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
}