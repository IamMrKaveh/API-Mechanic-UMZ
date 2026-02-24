namespace Application.User.Features.Commands.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (request.Dto.NewPassword != request.Dto.ConfirmNewPassword)
            return ServiceResult.Failure("رمز عبور جدید و تکرار آن یکسان نیستند.");

        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return ServiceResult.Failure("کاربر یافت نشد.", 404);

        try
        {
            user.ChangePassword(request.Dto.CurrentPassword, request.Dto.NewPassword);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }
    }
}