using Domain.Security.Aggregates;

namespace Domain.Security.Rules;

public sealed class SessionMustBeActiveRule : IBusinessRule
{
    private readonly UserSession _session;

    public SessionMustBeActiveRule(UserSession session)
    {
        _session = session;
    }

    public bool IsBroken()
    {
        return !_session.IsActive;
    }

    public string Message
    {
        get
        {
            if (_session.IsRevoked) return "نشست لغو شده است.";
            if (_session.IsExpired) return "نشست منقضی شده است.";
            return "نشست فعال نیست.";
        }
    }
}