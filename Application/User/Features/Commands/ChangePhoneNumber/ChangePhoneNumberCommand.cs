namespace Application.User.Features.Commands.ChangePhoneNumber;

public record ChangePhoneNumberCommand : IRequest<ServiceResult>
{
    public int UserId { get; init; }
    public required string NewPhoneNumber { get; init; }
    public required string OtpCode { get; init; }
}