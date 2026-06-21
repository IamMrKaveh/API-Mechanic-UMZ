using Domain.Security.Enums;

namespace Application.Auth.Features.Commands.SendOtp;

public record SendOtpCommand(
    string PhoneNumber,
    OtpPurpose Purpose = OtpPurpose.Login) : ICommand, IBypassTransactionBehavior;