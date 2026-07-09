using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletStatistics;

public sealed class GetWalletStatisticsHandler(IWalletQueryService walletQueryService)
    : IQueryHandler<GetWalletStatisticsQuery, WalletStatisticsDto>
{
    public async Task<ServiceResult<WalletStatisticsDto>> Handle(
        GetWalletStatisticsQuery request,
        CancellationToken ct)
    {
        var result = await walletQueryService.GetStatisticsAsync(ct);
        return ServiceResult<WalletStatisticsDto>.Success(result);
    }
}