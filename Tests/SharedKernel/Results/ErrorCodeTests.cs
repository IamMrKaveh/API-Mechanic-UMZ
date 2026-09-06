using System.Reflection;
using SharedKernel.Results;

namespace Tests.SharedKernel.Results;

public class ErrorCodeTests
{
    [Theory]
    [InlineData(nameof(ErrorCode.Validation), "GEN_VALIDATION")]
    [InlineData(nameof(ErrorCode.NotFound), "GEN_NOT_FOUND")]
    [InlineData(nameof(ErrorCode.Conflict), "GEN_CONFLICT")]
    [InlineData(nameof(ErrorCode.Unauthorized), "GEN_UNAUTHORIZED")]
    [InlineData(nameof(ErrorCode.Forbidden), "GEN_FORBIDDEN")]
    [InlineData(nameof(ErrorCode.RateLimitExceeded), "GEN_RATE_LIMIT")]
    [InlineData(nameof(ErrorCode.BusinessRule), "GEN_BUSINESS_RULE")]
    [InlineData(nameof(ErrorCode.Infrastructure), "GEN_INFRASTRUCTURE")]
    [InlineData(nameof(ErrorCode.ExternalService), "GEN_EXTERNAL_SERVICE")]
    [InlineData(nameof(ErrorCode.Unexpected), "GEN_UNEXPECTED")]
    [InlineData(nameof(ErrorCode.Failure), "GEN_FAILURE")]
    [InlineData(nameof(ErrorCode.ConcurrencyConflict), "GEN_CONCURRENCY_CONFLICT")]
    [InlineData(nameof(ErrorCode.UniqueViolation), "GEN_UNIQUE_VIOLATION")]
    [InlineData(nameof(ErrorCode.ForeignKeyViolation), "GEN_FOREIGN_KEY_VIOLATION")]
    public void ErrorCode_HasExpectedValue(string name, string expected)
    {
        var field = typeof(ErrorCode).GetField(name);

        field.ShouldNotBeNull();
        ((string)field!.GetValue(null)!).ShouldBe(expected);
    }

    [Fact]
    public void ErrorCodes_AreAllUniqueAndPrefixed()
    {
        var values = typeof(ErrorCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        values.Count.ShouldBe(14);
        values.Distinct().Count().ShouldBe(values.Count);
        foreach (var value in values)
            value.ShouldStartWith("GEN_");
    }
}
