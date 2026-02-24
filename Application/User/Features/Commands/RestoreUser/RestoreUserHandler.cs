namespace Application.User.Features.Commands.RestoreUser;

public class RestoreUserHandler : IRequestHandler<RestoreUserCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id); 
        if (user == null)
            return ServiceResult.Failure("NotFound");

        user.Restore();

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }
}