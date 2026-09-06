using SharedKernel.Results;

namespace Tests.SharedKernel.Results;

public class ResultTypeTests
{
    [Fact]
    public void ResultType_DefinesAllExpectedMembers()
    {
        Enum.GetNames<ResultType>().ShouldBe(
        [
            nameof(ResultType.Ok),
            nameof(ResultType.BadRequest),
            nameof(ResultType.NotFound),
            nameof(ResultType.Conflict),
            nameof(ResultType.Unauthorized),
            nameof(ResultType.Forbidden),
            nameof(ResultType.Unexpected),
            nameof(ResultType.RateLimitExceeded)
        ], ignoreOrder: true);
    }

    [Fact]
    public void ResultType_ValuesAreDistinct()
    {
        var values = Enum.GetValues<ResultType>().Select(v => (int)v).ToList();

        values.Distinct().Count().ShouldBe(values.Count);
    }

    [Theory]
    [InlineData(ResultType.Ok, 0)]
    [InlineData(ResultType.BadRequest, 1)]
    [InlineData(ResultType.NotFound, 2)]
    [InlineData(ResultType.Conflict, 3)]
    [InlineData(ResultType.Unauthorized, 4)]
    [InlineData(ResultType.Forbidden, 5)]
    [InlineData(ResultType.Unexpected, 6)]
    [InlineData(ResultType.RateLimitExceeded, 7)]
    public void ResultType_HasExpectedNumericValue(ResultType type, int expected)
    {
        ((int)type).ShouldBe(expected);
    }
}
