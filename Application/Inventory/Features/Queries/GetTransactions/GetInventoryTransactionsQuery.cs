namespace Application.Inventory.Features.Queries.GetTransactions;

public record GetInventoryTransactionsQuery(
    int? VariantId,
    string? TransactionType,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<InventoryTransactionDto>>>;