using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventoryTransactions;

public class GetInventoryTransactionsHandler(IInventoryQueryService queryService)
        : IRequestHandler<GetInventoryTransactionsQuery, ServiceResult<PaginatedResult<InventoryTransactionDto>>>
{
    public async Task<ServiceResult<PaginatedResult<InventoryTransactionDto>>> Handle(
        GetInventoryTransactionsQuery request,
        CancellationToken ct)
    {
        var result = await queryService.GetTransactionsPagedAsync(
            request.VariantId,
            request.TransactionType,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<InventoryTransactionDto>>.Success(result);
    }
}