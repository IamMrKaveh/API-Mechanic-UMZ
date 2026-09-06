using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;

namespace Tests.SharedKernel.Exceptions;

public class BusinessRuleViolationExceptionTests
{
    private sealed class AlwaysBrokenRule : IBusinessRule
    {
        public bool IsBroken() => true;
        public string Message => "rule was broken";
    }

    [Fact]
    public void Constructor_PreservesBrokenRule()
    {
        var rule = new AlwaysBrokenRule();

        var ex = new BusinessRuleViolationException(rule);

        ex.BrokenRule.ShouldBeSameAs(rule);
    }

    [Fact]
    public void Constructor_UsesRuleMessage()
    {
        var ex = new BusinessRuleViolationException(Substitute.For<IBusinessRule>());

        ex.Message.ShouldBe(ex.BrokenRule.Message);
    }

    [Fact]
    public void ErrorCode_IsAlwaysBusinessRuleViolation()
    {
        var rule = Substitute.For<IBusinessRule>();
        rule.Message.Returns("custom message");

        var ex = new BusinessRuleViolationException(rule);

        ex.ErrorCode.ShouldBe("BUSINESS_RULE_VIOLATION");
        ex.Message.ShouldBe("custom message");
    }

    [Fact]
    public void Exception_IsADomainException()
    {
        new BusinessRuleViolationException(new AlwaysBrokenRule())
            .ShouldBeAssignableTo<DomainException>();
    }
}
