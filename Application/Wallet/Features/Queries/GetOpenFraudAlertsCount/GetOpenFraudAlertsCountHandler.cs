using Application.Wallet.Contracts;

namespace Application.Wallet.Features.Queries.GetOpenFraudAlertsCount;

public sealed class GetOpenFraudAlertsCountHandler(IWalletFraudAlertQueryService queryService)
    : IRequestHandler<GetOpenFraudAlertsCountQuery, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(GetOpenFraudAlertsCountQuery request, CancellationToken ct)
    {
        var count = await queryService.GetOpenAlertsCountAsync(ct);
        return ServiceResult<int>.Success(count);
    }
}