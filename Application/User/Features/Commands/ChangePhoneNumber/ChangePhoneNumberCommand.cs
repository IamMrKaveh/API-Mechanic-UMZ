namespace Application.User.Features.Commands.ChangePhoneNumber;

public record ChangePhoneNumberCommand(
    string NewPhoneNumber,
    string OtpCode)
    : ICommand<ServiceResult>;