using Application.Common.Results;

namespace Application.User.Features.Commands.ChangePhoneNumber;

public record ChangePhoneNumberCommand(
    Guid UserId,
    string NewPhoneNumber,
    string OtpCode) : IRequest<ServiceResult>;