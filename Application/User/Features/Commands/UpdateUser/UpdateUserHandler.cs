namespace Application.User.Features.Commands.UpdateUser;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public UpdateUserHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IHtmlSanitizer htmlSanitizer)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<ServiceResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
            return ServiceResult.Failure("NotFound");

        if (user.IsDeleted)
            return ServiceResult.Failure("User account is deleted and cannot be modified.");

        user.UpdateName(
            !string.IsNullOrEmpty(request.UpdateRequest.FirstName) ? _htmlSanitizer.Sanitize(request.UpdateRequest.FirstName) : user.FirstName!,
            !string.IsNullOrEmpty(request.UpdateRequest.LastName) ? _htmlSanitizer.Sanitize(request.UpdateRequest.LastName) : user.LastName!
        );

        _userRepository.UpdateUser(user);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAdminEventAsync("UpdateUser", request.CurrentUserId, $"Updated profile for user {request.Id}");
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("User was modified by another process");
        }
    }
}