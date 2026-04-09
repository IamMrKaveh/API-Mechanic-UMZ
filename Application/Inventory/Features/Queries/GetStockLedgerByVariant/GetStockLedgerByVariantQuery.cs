using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetStockLedgerByVariant;

public record GetStockLedgerByVariantQuery(Guid VariantId) : IRequest<ServiceResult<PaginatedResult<StockLedgerEntryDto>>>;