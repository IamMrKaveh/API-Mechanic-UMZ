using SharedKernel.Results;

namespace Tests.SharedKernel.Results;

public class ErrorTypeTests
{
    [Theory]
    [InlineData(ErrorType.Failure, 0)]
    [InlineData(ErrorType.Validation, 1)]
    [InlineData(ErrorType.NotFound, 2)]
    [InlineData(ErrorType.Conflict, 3)]
    [InlineData(ErrorType.Unauthorized, 4)]
    [InlineData(ErrorType.Forbidden, 5)]
    [InlineData(ErrorType.RateLimitExceeded, 6)]
    [InlineData(ErrorType.BusinessRule, 7)]
    [InlineData(ErrorType.Infrastructure, 8)]
    [InlineData(ErrorType.ExternalService, 9)]
    [InlineData(ErrorType.Unexpected, 10)]
    public void ErrorType_HasExpectedNumericValue(ErrorType type, int expected)
    {
        ((int)type).ShouldBe(expected);
    }

    [Fact]
    public void ErrorType_DefinesAllExpectedMembers()
    {
        Enum.GetValues<ErrorType>().Length.ShouldBe(11);
    }

    [Fact]
    public void ErrorType_NamesMatchExpectedSet()
    {
        Enum.GetNames<ErrorType>().ShouldBe(
        [
            nameof(ErrorType.Failure),
            nameof(ErrorType.Validation),
            nameof(ErrorType.NotFound),
            nameof(ErrorType.Conflict),
            nameof(ErrorType.Unauthorized),
            nameof(ErrorType.Forbidden),
            nameof(ErrorType.RateLimitExceeded),
            nameof(ErrorType.BusinessRule),
            nameof(ErrorType.Infrastructure),
            nameof(ErrorType.ExternalService),
            nameof(ErrorType.Unexpected)
        ], ignoreOrder: true);
    }
}
