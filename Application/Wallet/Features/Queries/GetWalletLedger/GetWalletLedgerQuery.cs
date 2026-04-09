using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public record GetWalletLedgerQuery(Guid UserId) : IRequest<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>>;