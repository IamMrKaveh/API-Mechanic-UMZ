using Application.Common.Results;
using Application.Inventory.Features.Shared;
using SharedKernel.Models;

namespace Application.Inventory.Features.Queries.GetStockLedgerByVariant;

public record GetStockLedgerByVariantQuery(
    Guid VariantId,
    int Page = 1,
    int PageSize = 50) : IRequest<ServiceResult<PaginatedResult<StockLedgerEntryDto>>>;