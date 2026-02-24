namespace Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletHandler : IRequestHandler<CreditWalletCommand, ServiceResult<Unit>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreditWalletHandler> _logger;

    public CreditWalletHandler(
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreditWalletHandler> logger
        )
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<Unit>> Handle(
        CreditWalletCommand request,
        CancellationToken ct
        )
    {
        try
        {
            var alreadyProcessed = await _walletRepository.HasIdempotencyKeyAsync(request.UserId, request.IdempotencyKey, ct);
            if (alreadyProcessed)
            {
                _logger.LogInformation("Wallet credit idempotency key {Key} already processed for user {UserId}", request.IdempotencyKey, request.UserId);
                return ServiceResult<Unit>.Success(Unit.Value);
            }

            var wallet = await _walletRepository.GetByUserIdForUpdateAsync(request.UserId, ct);
            if (wallet == null)
            {
                wallet = Domain.Wallet.Wallet.Create(request.UserId);
                await _walletRepository.AddAsync(wallet, ct);
            }

            wallet.Credit(
                Money.FromDecimal(request.Amount),
                request.TransactionType,
                request.ReferenceType,
                request.ReferenceId,
                request.IdempotencyKey,
                request.CorrelationId,
                request.Description);

            _walletRepository.Update(wallet);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Wallet credited {Amount} for user {UserId} via {RefType}/{RefId}",
                request.Amount, request.UserId, request.ReferenceType, request.ReferenceId);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (Domain.Wallet.Exceptions.DuplicateWalletIdempotencyKeyException)
        {
            _logger.LogWarning("Duplicate idempotency key: {Key}", request.IdempotencyKey);
            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict crediting wallet for user {UserId}. Retry recommended.", request.UserId);
            return ServiceResult<Unit>.Failure("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.", 409);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crediting wallet for user {UserId}", request.UserId);
            return ServiceResult<Unit>.Failure("خطا در شارژ کیف پول.");
        }
    }
}