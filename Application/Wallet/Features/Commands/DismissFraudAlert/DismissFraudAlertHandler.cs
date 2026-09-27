using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.DismissFraudAlert;

public sealed class DismissFraudAlertHandler(
    IWalletFraudAlertRepository repository,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<DismissFraudAlertCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(DismissFraudAlertCommand request, CancellationToken ct)
    {
        try
        {
            var alertId = WalletFraudAlertId.From(request.AlertId);
            var adminId = UserId.From(currentUserService.UserId!.Value);

            var alertResult = await (repository.GetByIdAsync(alertId, ct)).OrNotFoundAsync("هشدار مورد نظر یافت نشد.");
            if (alertResult.IsFailure) return alertResult.Error;
            var alert = alertResult.Value;

            alert.Dismiss(adminId, request.Note, dateTimeProvider.UtcNow);

            repository.Update(alert);

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
    }
}
