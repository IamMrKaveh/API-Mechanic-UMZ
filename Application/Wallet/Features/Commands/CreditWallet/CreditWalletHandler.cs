using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreditWalletCommand, Unit>
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
                wallet = Domain.Wallet.Aggregates.Wallet.Create(userId);
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
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}