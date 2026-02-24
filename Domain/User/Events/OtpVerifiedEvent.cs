namespace Domain.User.Events;

public class OtpVerifiedEvent : DomainEvent
{
    public int UserId { get; }

    public OtpVerifiedEvent(int userId)
    {
        UserId = userId;
    }
}