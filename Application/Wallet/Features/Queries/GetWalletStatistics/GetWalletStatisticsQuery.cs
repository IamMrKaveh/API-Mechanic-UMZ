using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletStatistics;

public sealed record GetWalletStatisticsQuery() : IQuery<WalletStatisticsDto>;