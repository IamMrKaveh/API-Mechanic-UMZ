namespace Application.Wallet.Features.Commands.ReserveWallet;

public class ReserveWalletHandler : IRequestHandler<ReserveWalletCommand, ServiceResult<Unit>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReserveWalletHandler> _logger;

    public ReserveWalletHandler(
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReserveWalletHandler> logger
        )
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<Unit>> Handle(
        ReserveWalletCommand request,
        CancellationToken ct
        )
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdForUpdateAsync(request.UserId, ct);
            if (wallet == null)
                return ServiceResult<Unit>.Failure("کیف پول یافت نشد.", 404);

            wallet.Reserve(Money.FromDecimal(request.Amount), request.OrderId, request.ExpiresAt);
            _walletRepository.Update(wallet);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (Domain.Wallet.Exceptions.InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message, 422);
        }
        catch (ConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict reserving wallet for user {UserId}. Retry recommended.", request.UserId);
            return ServiceResult<Unit>.Failure("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.", 409);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving wallet for user {UserId}", request.UserId);
            return ServiceResult<Unit>.Failure("خطا در رزرو کیف پول.");
        }
    }
}