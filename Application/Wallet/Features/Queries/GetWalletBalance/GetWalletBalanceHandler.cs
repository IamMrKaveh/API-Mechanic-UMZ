using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Queries.GetWalletBalance;

public sealed class GetWalletBalanceHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetWalletBalanceQuery, WalletDto>
{
    public async Task<ServiceResult<WalletDto>> Handle(
        GetWalletBalanceQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.userId ?? currentUserService.UserId.Value);

        var wallet = await walletRepository.GetByUserIdAsync(userId, ct);

        if (wallet is null)
        {
            wallet = Domain.Wallet.Aggregates.Wallet.Create(userId);
            await walletRepository.AddAsync(wallet, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

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