namespace Application.Common.Interfaces.Admin;

public interface IAdminInventoryService
{
    Task<ServiceResult<PagedResultDto<InventoryTransactionDto>>> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize);
    Task<ServiceResult<IEnumerable<LowStockItemDto>>> GetLowStockItemsAsync(int threshold);
    Task<ServiceResult<IEnumerable<OutOfStockItemDto>>> GetOutOfStockItemsAsync();
    Task<ServiceResult> AdjustStockAsync(StockAdjustmentDto dto, int userId);
    Task<ServiceResult<InventoryStatisticsDto>> GetStatisticsAsync();
}