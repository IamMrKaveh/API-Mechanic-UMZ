using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.DebitWallet;

public class DebitWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<DebitWalletHandler> logger) : IRequestHandler<DebitWalletCommand, ServiceResult<Unit>>
{
    private readonly IWalletRepository _walletRepository = walletRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DebitWalletHandler> _logger = logger;

    public async Task<ServiceResult<Unit>> Handle(
        DebitWalletCommand request,
        CancellationToken ct)
    {
        try
        {
            var alreadyProcessed = await _walletRepository.HasIdempotencyKeyAsync(request.UserId, request.IdempotencyKey, ct);
            if (alreadyProcessed)
                return ServiceResult<Unit>.Success(Unit.Value);

            var wallet = await _walletRepository.GetByUserIdForUpdateAsync(request.UserId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول یافت نشد.");

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
        catch (DbUpdateException)
        {
            _logger.LogWarning(
                "Duplicate idempotency key (DB constraint) on debit: {Key} for user {UserId}",
                request.IdempotencyKey, request.UserId);
            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Unexpected(ex.Message);
        }
        catch (ConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict debiting wallet for user {UserId}. Retry recommended.",
                request.UserId);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Unexpected(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debiting wallet for user {UserId}", request.UserId);
            return ServiceResult<Unit>.Unexpected("خطا در برداشت از کیف پول.");
        }
    }
}