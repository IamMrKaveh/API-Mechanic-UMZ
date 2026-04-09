using Application.Wallet.Features.Shared;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Queries.GetWalletBalance;

public class GetWalletBalanceHandler(IWalletRepository walletRepository, IUnitOfWork unitOfWork) : IRequestHandler<GetWalletBalanceQuery, ServiceResult<WalletDto>>
{
    private readonly IWalletRepository _walletRepository = walletRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<WalletDto>> Handle(GetWalletBalanceQuery request, CancellationToken ct)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(request.UserId, ct);

        if (wallet == null)
        {
            wallet = Domain.Wallet.Aggregates.Wallet.Create(request.UserId);
            await _walletRepository.AddAsync(wallet, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var dto = new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            CurrentBalance = wallet.CurrentBalance,
            ReservedBalance = wallet.ReservedBalance,
            AvailableBalance = wallet.AvailableBalance,
            Status = wallet.Status.ToString()
        };

        return ServiceResult<WalletDto>.Success(dto);
    }
}