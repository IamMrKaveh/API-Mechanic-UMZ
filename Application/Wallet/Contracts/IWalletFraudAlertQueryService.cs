using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;

namespace Application.Wallet.Contracts;

public interface IWalletFraudAlertQueryService
{
    Task<PaginatedResult<WalletFraudAlertDto>> GetAlertsPageAsync(
        FraudAlertStatus? status,
        FraudAlertSeverity? severity,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<WalletFraudAlertDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<int> GetOpenAlertsCountAsync(CancellationToken ct = default);
}