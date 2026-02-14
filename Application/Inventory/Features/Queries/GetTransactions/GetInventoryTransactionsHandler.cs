namespace Application.Inventory.Features.Queries.GetTransactions;

public class GetInventoryTransactionsHandler
    : IRequestHandler<GetInventoryTransactionsQuery, ServiceResult<PaginatedResult<InventoryTransactionDto>>>
{
    private readonly IInventoryQueryService _queryService;

    public GetInventoryTransactionsHandler(IInventoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<InventoryTransactionDto>>> Handle(
        GetInventoryTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _queryService.GetTransactionsAsync(
            request.VariantId,
            request.TransactionType,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<InventoryTransactionDto>>.Success(result);
    }
}