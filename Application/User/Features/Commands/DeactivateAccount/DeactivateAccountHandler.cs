using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeactivateAccount;

public class DeactivateAccountHandler(
    IUserRepository userRepository,
    ISessionService sessionService,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : ICommandHandler<DeactivateAccountCommand>
{
    public async Task<ServiceResult> Handle(
        DeactivateAccountCommand request,
        CancellationToken ct)
    {
        var currentUserId = UserId.From(currentUser.UserId!.Value);
        var userResult = await (userRepository.GetByIdAsync(currentUserId, ct)).OrNotFoundAsync("کاربر یافت نشد.");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        try
        {
            user.Deactivate();
            userRepository.Update(user);

            await sessionService.RevokeAllSessionsAsync(user.Id, ct);

            await auditService.LogSecurityEventAsync(
                "AccountDeactivated",
                $"حساب کاربر {user.Id} غیرفعال شد.",
                IpAddress.Unknown,
                user.Id,
                ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}
