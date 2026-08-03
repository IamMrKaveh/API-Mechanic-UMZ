using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetAdminDebitRequests;

public sealed class GetAdminDebitRequestsHandler(IWalletDebitRequestQueryService queryService)
    : IQueryHandler<GetAdminDebitRequestsQuery, PaginatedResult<AdminDebitRequestListItemDto>>
{
    public async Task<ServiceResult<PaginatedResult<AdminDebitRequestListItemDto>>> Handle(
        GetAdminDebitRequestsQuery request,
        CancellationToken ct)
    {
        var filter = new WalletDebitRequestFilter
        {
            OwnerId = request.OwnerId,
            RequestedBy = request.RequestedBy,
            Status = request.Status,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        var result = await queryService.GetPageAsync(request.Page, request.PageSize, filter, ct);
        return ServiceResult<PaginatedResult<AdminDebitRequestListItemDto>>.Success(result);
    }
}
