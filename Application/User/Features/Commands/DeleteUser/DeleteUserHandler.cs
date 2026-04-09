using Domain.User.Interfaces;

namespace Application.User.Features.Commands.DeleteUser;

public class DeleteUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IAuditService auditService) : IRequestHandler<DeleteUserCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;

    public async Task<ServiceResult> Handle(
        DeleteUserCommand request,
        CancellationToken ct)
    {
        if (request.Id.Value == request.CurrentUserId)
            return ServiceResult.Forbidden("Admins cannot delete their own account this way.");

        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
            return ServiceResult.NotFound("User Not Found");

        user.Delete(request.CurrentUserId);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogAdminEventAsync(
            "DeleteUser",
            request.CurrentUserId,
            $"Soft-deleted user {request.Id}");

        return ServiceResult.Success();
    }
}