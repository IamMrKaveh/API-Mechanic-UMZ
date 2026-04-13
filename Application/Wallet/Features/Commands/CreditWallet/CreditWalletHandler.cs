using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<CreditWalletCommand, ServiceResult<Unit>>
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
                return ServiceResult<Unit>.Success(Unit.Value);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
            {
                wallet = Domain.Wallet.Aggregates.Wallet.Create(WalletId.NewId(), userId);
                await walletRepository.AddAsync(wallet, ct);
            }

            wallet.Credit(
                Money.FromDecimal(request.Amount),
                request.Description ?? request.TransactionType.ToString(),
                request.ReferenceId);

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletCreditConcurrencyConflict",
                $"تعارض همزمانی در شارژ کیف پول. IdempotencyKey: {request.IdempotencyKey}",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}