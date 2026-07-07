using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetFraudAlertById;

public sealed record GetFraudAlertByIdQuery(Guid AlertId) : IQuery<WalletFraudAlertDto>;