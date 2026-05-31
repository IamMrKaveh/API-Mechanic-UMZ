using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Queries.GetWalletBalance;

public class GetWalletBalanceHandler(
    IWalletRepository walletRepository)
    : IRequestHandler<GetWalletBalanceQuery, ServiceResult<WalletDto>>
{
    public async Task<ServiceResult<WalletDto>> Handle(
        GetWalletBalanceQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var wallet = await walletRepository.GetByUserIdAsync(userId, ct);

        if (wallet is null)
            return ServiceResult<WalletDto>.NotFound("کیف پول یافت نشد.");

        return ServiceResult<WalletDto>.Success(new WalletDto
        {
            Id = wallet.Id.Value,
            UserId = wallet.OwnerId.Value,
            CurrentBalance = wallet.Balance.Amount,
            ReservedBalance = wallet.ReservedBalance.Amount,
            AvailableBalance = wallet.AvailableBalance.Amount
        });
    }
}