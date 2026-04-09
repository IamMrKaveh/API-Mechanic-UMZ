namespace Domain.Common.Exceptions;

public sealed class BusinessRuleViolationException : DomainException
{
    public IBusinessRule BrokenRule { get; }

    public override string ErrorCode => "BUSINESS_RULE_VIOLATION";

    public BusinessRuleViolationException(IBusinessRule brokenRule)
        : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }
}