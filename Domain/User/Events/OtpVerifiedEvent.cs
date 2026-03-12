namespace Domain.User.Events;

public class OtpVerifiedEvent(UserId userId) : DomainEvent
{
    public UserId UserId { get; } = userId;
}