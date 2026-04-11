using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateUser;

public class UpdateUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<UpdateUserCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateUserCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.Id);
        var adminId = UserId.From(request.CurrentUserId);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            return ServiceResult.NotFound("NotFound");

        if (user.IsDeleted)
            return ServiceResult.Forbidden("User account is deleted and cannot be modified.");

        user.UpdateName(
            !string.IsNullOrEmpty(request.FirstName)
                ? request.FirstName
                : user.FirstName!,
            !string.IsNullOrEmpty(request.LastName)
                ? request.LastName
                : user.LastName!
        );

        userRepository.Update(user);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            await auditService.LogAdminEventAsync(
                "UpdateUser",
                adminId,
                $"Updated profile for user {request.Id}");
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("User was modified by another process");
        }
    }
}