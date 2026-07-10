using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public sealed class MarkFraudAlertReviewedHandler(
    IWalletFraudAlertRepository repository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<MarkFraudAlertReviewedCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(MarkFraudAlertReviewedCommand request, CancellationToken ct)
    {
        try
        {
            var alertId = WalletFraudAlertId.From(request.AlertId);
            var adminId = UserId.From(request.AdminId);

            var alert = await repository.GetByIdAsync(alertId, ct);
            if (alert is null)
                return ServiceResult<Unit>.NotFound("هشدار مورد نظر یافت نشد.");

            alert.MarkAsReviewed(adminId, request.Note);

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