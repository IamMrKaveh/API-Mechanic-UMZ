using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.FreezeWallet;

public sealed class FreezeWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<FreezeWalletCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        FreezeWalletCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);
            var adminId = UserId.From(request.AdminId);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            wallet.Freeze(request.Reason, adminId);

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (WalletInactiveException)
        {
            return ServiceResult<Unit>.Conflict("کیف پول در حال حاضر مسدود است.");
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