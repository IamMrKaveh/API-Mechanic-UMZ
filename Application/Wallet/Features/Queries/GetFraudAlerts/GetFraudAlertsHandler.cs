using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetFraudAlerts;

public sealed class GetFraudAlertsHandler(IWalletFraudAlertQueryService queryService)
    : IRequestHandler<GetFraudAlertsQuery, ServiceResult<PaginatedResult<WalletFraudAlertDto>>>
{
    public async Task<ServiceResult<PaginatedResult<WalletFraudAlertDto>>> Handle(
        GetFraudAlertsQuery request,
        CancellationToken ct)
    {
        var result = await queryService.GetAlertsPageAsync(
            request.Status,
            request.Severity,
            request.UserId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<WalletFraudAlertDto>>.Success(result);
    }
}