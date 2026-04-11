using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Queries.GetWalletBalance;

public class GetWalletBalanceHandler(IWalletRepository walletRepository, IUnitOfWork unitOfWork) : IRequestHandler<GetWalletBalanceQuery, ServiceResult<WalletDto>>
{
    public async Task<ServiceResult<WalletDto>> Handle(GetWalletBalanceQuery request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var wallet = await walletRepository.GetByUserIdAsync(userId, ct);

        if (wallet is null)
        {
            wallet = Domain.Wallet.Aggregates.Wallet.Create(
                WalletId.NewId(),
                userId);
            await walletRepository.AddAsync(wallet, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        var dto = new WalletDto
        {
            Id = wallet.Id.Value,
            UserId = wallet.OwnerId.Value,
            CurrentBalance = wallet.Balance.Amount,
            ReservedBalance = wallet.ReservedBalance.Amount,
            AvailableBalance = wallet.AvailableBalance.Amount
        };

        return ServiceResult<WalletDto>.Success(dto);
    }
}