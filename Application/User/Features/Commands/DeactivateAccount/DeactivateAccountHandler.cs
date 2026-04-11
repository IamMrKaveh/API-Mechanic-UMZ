using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeactivateAccount;

public class DeactivateAccountHandler(
    IUserRepository userRepository,
    ISessionService sessionService,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeactivateAccountCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        DeactivateAccountCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        try
        {
            user.Deactivate();

            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(ct);

            await sessionService.RevokeAllSessionsAsync(userId, ct);

            await auditService.LogSecurityEventAsync(
                "AccountDeactivated",
                $"حساب کاربر {userId} غیرفعال شد.",
                IpAddress.Unknown,
                userId,
                ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}