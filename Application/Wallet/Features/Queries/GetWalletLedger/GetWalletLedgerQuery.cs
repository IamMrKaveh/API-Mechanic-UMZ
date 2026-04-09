using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public record GetWalletLedgerQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>>;