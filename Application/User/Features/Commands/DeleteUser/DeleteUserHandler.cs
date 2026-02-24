namespace Application.User.Features.Commands.DeleteUser;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public DeleteUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<ServiceResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == request.CurrentUserId)
        {
            return ServiceResult.Failure("Admins cannot delete their own account this way.");
        }

        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null) return ServiceResult.Failure("NotFound");

        user.Delete(request.CurrentUserId);

        _userRepository.UpdateUser(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAdminEventAsync("DeleteUser", request.CurrentUserId, $"Soft-deleted user {request.Id}");

        return ServiceResult.Success();
    }
}