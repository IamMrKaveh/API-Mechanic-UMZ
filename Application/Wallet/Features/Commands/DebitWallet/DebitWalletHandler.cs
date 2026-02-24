namespace Application.Wallet.Features.Commands.DebitWallet;

public class DebitWalletHandler : IRequestHandler<DebitWalletCommand, ServiceResult<Unit>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DebitWalletHandler> _logger;

    public DebitWalletHandler(
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        ILogger<DebitWalletHandler> logger)
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<Unit>> Handle(DebitWalletCommand request, CancellationToken ct)
    {
        try
        {
            var alreadyProcessed = await _walletRepository.HasIdempotencyKeyAsync(request.UserId, request.IdempotencyKey, ct);
            if (alreadyProcessed)
                return ServiceResult<Unit>.Success(Unit.Value);

            var wallet = await _walletRepository.GetByUserIdWithEntriesAsync(request.UserId, ct);
            if (wallet == null)
                return ServiceResult<Unit>.Failure("کیف پول یافت نشد.", 404);

            wallet.Debit(
                Money.FromDecimal(request.Amount),
                request.TransactionType,
                request.ReferenceType,
                request.ReferenceId,
                request.IdempotencyKey,
                request.CorrelationId,
                request.Description);

            _walletRepository.Update(wallet);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (Domain.Wallet.Exceptions.InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message, 422);
        }
        catch (Domain.Wallet.Exceptions.DuplicateWalletIdempotencyKeyException)
        {
            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debiting wallet for user {UserId}", request.UserId);
            return ServiceResult<Unit>.Failure("خطا در برداشت از کیف پول.");
        }
    }
}