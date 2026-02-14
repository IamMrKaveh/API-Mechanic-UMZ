namespace Domain.Common.Exceptions;

public class BusinessRuleViolationException : DomainException
{
    public IBusinessRule BrokenRule { get; }

    public BusinessRuleViolationException(IBusinessRule brokenRule)
        : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }
}