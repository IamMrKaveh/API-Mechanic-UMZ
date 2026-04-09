using Application.Common.Results;
using Application.Inventory.Features.Shared;
using SharedKernel.Models;

namespace Application.Inventory.Features.Queries.GetInventoryTransactions;

public record GetInventoryTransactionsQuery(
    Guid? VariantId,
    string? TransactionType,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<InventoryTransactionDto>>>;