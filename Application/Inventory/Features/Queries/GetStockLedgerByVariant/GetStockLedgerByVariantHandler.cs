using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetStockLedgerByVariant;

public class GetStockLedgerByVariantHandler(
    IStockLedgerQueryService ledgerQueryService)
    : IRequestHandler<GetStockLedgerByVariantQuery, ServiceResult<PaginatedResult<StockLedgerEntryDto>>>
{
    public async Task<ServiceResult<PaginatedResult<StockLedgerEntryDto>>> Handle(
        GetStockLedgerByVariantQuery request,
        CancellationToken ct)
    {
        var result = await ledgerQueryService.GetByVariantIdAsync(
            Domain.Variant.ValueObjects.VariantId.From(request.VariantId),
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<StockLedgerEntryDto>>.Success(result);
    }
}