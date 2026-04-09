using Application.Common.Results;
using Application.Wallet.Features.Shared;
using SharedKernel.Models;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public record GetWalletLedgerQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>>;