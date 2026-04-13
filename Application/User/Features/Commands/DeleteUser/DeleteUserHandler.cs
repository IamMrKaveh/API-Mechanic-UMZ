using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeleteUser;

public class DeleteUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeleteUserCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        DeleteUserCommand request,
        CancellationToken ct)
    {
        if (request.Id == request.CurrentUserId)
            return ServiceResult.Forbidden("Admins cannot delete their own account this way.");

        var userId = UserId.From(request.Id);
        var currentUserId = UserId.From(request.CurrentUserId);

        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return ServiceResult.NotFound("User Not Found");

        user.Deactivate();

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAdminEventAsync(
            "DeleteUser",
            currentUserId,
            $"Soft-deleted user {request.Id}");

        return ServiceResult.Success();
    }
}