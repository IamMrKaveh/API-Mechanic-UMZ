namespace Application.Wallet.Features.Queries.GetWalletBalance;

public class GetWalletBalanceHandler : IRequestHandler<GetWalletBalanceQuery, ServiceResult<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetWalletBalanceHandler(
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork
        )
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<WalletDto>> Handle(
        GetWalletBalanceQuery request,
        CancellationToken ct
        )
    {
        var wallet = await _walletRepository.GetByUserIdAsync(request.UserId, ct);

        if (wallet == null)
        {
            wallet = Domain.Wallet.Wallet.Create(request.UserId);
            await _walletRepository.AddAsync(wallet, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var dto = new WalletDto(
            wallet.UserId,
            wallet.CurrentBalance,
            wallet.ReservedBalance,
            wallet.AvailableBalance,
            wallet.Status.ToString());

        return ServiceResult<WalletDto>.Success(dto);
    }
}