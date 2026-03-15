using Domain.Security.Aggregates;

namespace Domain.Security.Specifications;

public class ExpiredSessionSpecification : Specification<UserSession>
{
    public override Expression<Func<UserSession, bool>> ToExpression()
    {
        return s => !s.IsRevoked && s.ExpiresAt <= DateTime.UtcNow;
    }
}