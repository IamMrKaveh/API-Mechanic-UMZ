using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetAdminDebitRequests;

public sealed record GetAdminDebitRequestsQuery(
    Guid? OwnerId = null,
    Guid? RequestedBy = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20)
    : IPageQuery<AdminDebitRequestListItemDto>;
