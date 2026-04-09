using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetStockLedgerByVariant;

public record GetStockLedgerByVariantQuery(
    Guid VariantId,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<StockLedgerEntryDto>>>;