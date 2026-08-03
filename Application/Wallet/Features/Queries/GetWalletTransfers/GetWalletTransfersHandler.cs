using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletTransfers;

public sealed class GetWalletTransfersHandler(IWalletTransferQueryService queryService)
    : IQueryHandler<GetWalletTransfersQuery, PaginatedResult<WalletTransferDto>>
{
    public async Task<ServiceResult<PaginatedResult<WalletTransferDto>>> Handle(
        GetWalletTransfersQuery request,
        CancellationToken ct)
    {
        var filter = new WalletTransferFilter
        {
            UserId = request.UserId,
            Status = request.Status,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        var result = await queryService.GetTransfersPageAsync(request.Page, request.PageSize, filter, ct);
        return ServiceResult<PaginatedResult<WalletTransferDto>>.Success(result);
    }
}
