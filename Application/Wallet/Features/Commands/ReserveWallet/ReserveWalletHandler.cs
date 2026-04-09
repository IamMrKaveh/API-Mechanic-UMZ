using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.ReserveWallet;

public class ReserveWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReserveWalletHandler> logger
        ) : IRequestHandler<ReserveWalletCommand, ServiceResult<Unit>>
{
    private readonly IWalletRepository _walletRepository = walletRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<ReserveWalletHandler> _logger = logger;

    public async Task<ServiceResult<Unit>> Handle(
        ReserveWalletCommand request,
        CancellationToken ct)
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdForUpdateAsync(request.UserId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول یافت نشد.");

            wallet.Reserve(Money.FromDecimal(request.Amount), request.WalletId, request.ExpiresAt);
            _walletRepository.Update(wallet);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Unexpected(ex.Message);
        }
        catch (ConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict reserving wallet for user {UserId}. Retry recommended.",
                request.UserId);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Unexpected(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving wallet for user {UserId}", request.UserId);
            return ServiceResult<Unit>.Unexpected("خطا در رزرو کیف پول.");
        }
    }
}