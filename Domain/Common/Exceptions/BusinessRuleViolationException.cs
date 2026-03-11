namespace Domain.Common.Exceptions;

public class BusinessRuleViolationException(IBusinessRule brokenRule) : DomainException(brokenRule.Message)
{
    public IBusinessRule BrokenRule { get; } = brokenRule;
}