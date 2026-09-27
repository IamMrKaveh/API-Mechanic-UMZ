using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public sealed class MarkFraudAlertReviewedHandler(
    IWalletFraudAlertRepository repository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<MarkFraudAlertReviewedCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(MarkFraudAlertReviewedCommand request, CancellationToken ct)
    {
        try
        {
            var alertId = WalletFraudAlertId.From(request.AlertId);
            var adminId = UserId.From(currentUserService.UserId!.Value);

            var alertResult = await (repository.GetByIdAsync(alertId, ct)).OrNotFoundAsync("هشدار مورد نظر یافت نشد.");
            if (alertResult.IsFailure) return alertResult.Error;
            var alert = alertResult.Value;

            alert.MarkAsReviewed(adminId, request.Note, dateTimeProvider.UtcNow);

            repository.Update(alert);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "FraudAlertReviewed",
                $"هشدار {alertId.Value} توسط ادمین {adminId.Value} بررسی شد.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
