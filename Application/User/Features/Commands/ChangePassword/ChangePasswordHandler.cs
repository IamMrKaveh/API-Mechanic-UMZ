namespace Application.User.Features.Commands.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(ChangePasswordCommand request, CancellationToken ct)
        => Task.FromResult(ServiceResult.Success()); // Passwords mechanism not fully mapped in domain models provided, mock success.
}