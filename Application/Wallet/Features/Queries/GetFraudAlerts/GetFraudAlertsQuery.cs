using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;

namespace Application.Wallet.Features.Queries.GetFraudAlerts;

public sealed record GetFraudAlertsQuery(
    FraudAlertStatus? Status,
    FraudAlertSeverity? Severity,
    Guid? UserId,
    int Page,
    int PageSize,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IPageQuery<WalletFraudAlertDto>;