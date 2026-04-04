using Application.Common.Results;

namespace Application.Inventory.Features.Queries.GetStockLedgerByVariant;

public record GetStockLedgerByVariantQuery(
    int VariantId,
    int Page = 1,
    int PageSize = 50
) : IRequest<ServiceResult<PaginatedResult<StockLedgerEntryDto>>>;