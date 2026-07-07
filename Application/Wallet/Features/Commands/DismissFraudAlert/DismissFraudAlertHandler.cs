using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.DismissFraudAlert;

public sealed class DismissFraudAlertHandler(
    IWalletFraudAlertRepository repository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<DismissFraudAlertCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(DismissFraudAlertCommand request, CancellationToken ct)
    {
        try
        {
            var alertId = WalletFraudAlertId.From(request.AlertId);
            var adminId = UserId.From(request.AdminId);

            var alert = await repository.GetByIdAsync(alertId, ct);
            if (alert is null)
                return ServiceResult<Unit>.NotFound("هشدار مورد نظر یافت نشد.");

            alert.Dismiss(adminId, request.Note);

            repository.Update(alert);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "FraudAlertDismissed",
                $"هشدار {alertId.Value} توسط ادمین {adminId.Value} رد شد.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return ServiceResult<Unit>.Failure("خطا در پردازش هشدار.");
        }
    }
}