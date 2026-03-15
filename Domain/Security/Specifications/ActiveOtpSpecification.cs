using Domain.Security.Aggregates;

namespace Domain.Security.Specifications;

public class ActiveOtpSpecification : Specification<UserOtp>
{
    public override Expression<Func<UserOtp, bool>> ToExpression()
    {
        return o => !o.IsVerified && o.ExpiresAt > DateTime.UtcNow;
    }
}