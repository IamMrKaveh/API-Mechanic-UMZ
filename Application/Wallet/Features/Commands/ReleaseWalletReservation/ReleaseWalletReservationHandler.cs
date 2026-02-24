namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public class ReleaseWalletReservationHandler : IRequestHandler<ReleaseWalletReservationCommand, ServiceResult<Unit>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReleaseWalletReservationHandler> _logger;

    public ReleaseWalletReservationHandler(
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReleaseWalletReservationHandler> logger
        )
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<Unit>> Handle(
        ReleaseWalletReservationCommand request,
        CancellationToken ct
        )
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdForUpdateAsync(request.UserId, ct);
            if (wallet == null)
                return ServiceResult<Unit>.Success(Unit.Value);

            wallet.ReleaseReservation(request.OrderId);
            _walletRepository.Update(wallet);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict releasing wallet reservation for order {OrderId}.", request.OrderId);
            return ServiceResult<Unit>.Failure("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.", 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing wallet reservation for order {OrderId}", request.OrderId);
            return ServiceResult<Unit>.Failure("خطا در آزادسازی رزرو کیف پول.");
        }
    }
}