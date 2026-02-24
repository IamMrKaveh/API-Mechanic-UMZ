namespace Application.Wallet.Features.Queries.GetWalletLedger;

public class GetWalletLedgerHandler : IRequestHandler<GetWalletLedgerQuery, ServiceResult<PaginatedResult<WalletLedgerEntryDto>>>
{
    private readonly IWalletRepository _walletRepository;

    public GetWalletLedgerHandler(
        IWalletRepository walletRepository
        )
    {
        _walletRepository = walletRepository;
    }

    public async Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> Handle(
        GetWalletLedgerQuery request,
        CancellationToken ct
        )
    {
        var (items, total) = await _walletRepository.GetLedgerPageAsync(request.UserId, request.Page, request.PageSize, ct);

        var entries = items.Select(e => new WalletLedgerEntryDto(
            e.Id,
            e.AmountDelta,
            e.BalanceAfter,
            e.TransactionType.ToString(),
            e.ReferenceType.ToString(),
            e.ReferenceId,
            e.Description,
            e.CreatedAt))
        .ToList();

        var result = PaginatedResult<WalletLedgerEntryDto>.Create(entries, total, request.Page, request.PageSize);

        return ServiceResult<PaginatedResult<WalletLedgerEntryDto>>.Success(result);
    }
}