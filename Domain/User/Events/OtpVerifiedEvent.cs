namespace Domain.User.Events;

public class OtpVerifiedEvent(int userId) : DomainEvent
{
    public int UserId { get; } = userId;
}