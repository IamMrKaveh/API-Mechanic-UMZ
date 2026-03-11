using Application.Common.Models;

namespace Application.Inventory.Features.Queries.GetStockLedgerByVariant;

public class GetStockLedgerByVariantHandler(
    IStockLedgerQueryService ledgerQueryService)
        : IRequestHandler<GetStockLedgerByVariantQuery, ServiceResult<PaginatedResult<StockLedgerEntryDto>>>
{
    private readonly IStockLedgerQueryService _ledgerQueryService = ledgerQueryService;

    public async Task<ServiceResult<PaginatedResult<StockLedgerEntryDto>>> Handle(
        GetStockLedgerByVariantQuery request,
        CancellationToken ct)
    {
        var totalCount = await _ledgerQueryService.GetStockLedgerTotalCountAsync(
            request.VariantId, ct);

        var entries = await _ledgerQueryService.GetLedgerAsync(
            request.VariantId,
            page: request.Page,
            pageSize: request.PageSize,
            ct: ct);

        var result = PaginatedResult<StockLedgerEntryDto>.Create(
            entries.ToList(), totalCount, request.Page, request.PageSize);

        return ServiceResult<PaginatedResult<StockLedgerEntryDto>>.Success(result);
    }
}