using Application.Common.Exceptions;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.ReleaseWalletReservation;

public class ReleaseWalletReservationHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReleaseWalletReservationHandler> logger) : IRequestHandler<ReleaseWalletReservationCommand, ServiceResult<Unit>>
{
    private readonly IWalletRepository _walletRepository = walletRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<ReleaseWalletReservationHandler> _logger = logger;

    public async Task<ServiceResult<Unit>> Handle(
        ReleaseWalletReservationCommand request,
        CancellationToken ct)
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdForUpdateAsync(request.UserId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.Success(Unit.Value);

            wallet.ReleaseReservation(request.WalletReservationId);
            _walletRepository.Update(wallet);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict releasing wallet reservation for order {OrderId}.",
                request.WalletReservationId);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error releasing wallet reservation for order {OrderId}",
                request.WalletReservationId);
            return ServiceResult<Unit>.Unexpected("خطا در آزادسازی رزرو کیف پول.");
        }
    }
}