using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreditWalletHandler> logger) : IRequestHandler<CreditWalletCommand, ServiceResult<Unit>>
{
    public async Task<ServiceResult<Unit>> Handle(
        CreditWalletCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);

            var alreadyProcessed = await walletRepository.HasIdempotencyKeyAsync(userId, request.IdempotencyKey, ct);
            if (alreadyProcessed)
            {
                logger.LogInformation(
                    "Wallet credit idempotency key {Key} already processed for user {UserId}",
                    request.IdempotencyKey, request.UserId);
                return ServiceResult<Unit>.Success(Unit.Value);
            }

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
            {
                wallet = Domain.Wallet.Aggregates.Wallet.Create(
                    WalletId.NewId(),
                    userId);
                await walletRepository.AddAsync(wallet, ct);
            }

            wallet.Credit(
                Money.FromDecimal(request.Amount),
                request.Description,
                request.ReferenceId);

            walletRepository.Update(wallet);

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Wallet credited {Amount} for user {UserId} via {RefType}/{RefId}",
                request.Amount, request.UserId, request.ReferenceType, request.ReferenceId);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (DbUpdateException)
        {
            logger.LogWarning(
                "Duplicate idempotency key (DB constraint) on credit: {Key} for user {UserId}",
                request.IdempotencyKey, request.UserId);
            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            logger.LogWarning(
                "Concurrency conflict crediting wallet for user {UserId}. Retry recommended.",
                request.UserId);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}