using Application.Wallet.Contracts;
using Application.Common.Interfaces;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetFraudAlerts;

public sealed class GetFraudAlertsHandler(
    IWalletFraudAlertQueryService queryService,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetFraudAlertsQuery, ServiceResult<PaginatedResult<WalletFraudAlertDto>>>
{
    public async Task<ServiceResult<PaginatedResult<WalletFraudAlertDto>>> Handle(
        GetFraudAlertsQuery request,
        CancellationToken ct)
    {
        var result = await queryService.GetAlertsPageAsync(
            request.Status,
            request.Severity,
            WalletUserResolver.Resolve(currentUserService, request.UserId),
            request.Page,
            request.PageSize,
            request.FromDate,
            request.ToDate,
            ct);

        return ServiceResult<PaginatedResult<WalletFraudAlertDto>>.Success(result);
    }
}