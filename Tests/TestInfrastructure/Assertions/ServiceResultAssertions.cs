using SharedKernel.Results;

namespace Tests.TestInfrastructure.Assertions;

public static class ServiceResultAssertions
{
    public static ServiceResult ShouldBeSuccess(this ServiceResult result)
    {
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue(
            $"Expected ServiceResult to be Success but was Failure: [{result.Error.Code}] {result.Error.Message}");
        result.Error.ShouldBe(Error.None);
        return result;
    }

    public static ServiceResult<T> ShouldBeSuccess<T>(this ServiceResult<T> result)
    {
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue(
            $"Expected ServiceResult<{typeof(T).Name}> to be Success but was Failure: [{result.Error.Code}] {result.Error.Message}");
        result.Error.ShouldBe(Error.None);
        return result;
    }

    public static ServiceResult ShouldFailWith(this ServiceResult result, string expectedErrorCode)
    {
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue("Expected ServiceResult to be Failure but was Success.");
        result.Error.Code.ShouldBe(expectedErrorCode);
        return result;
    }

    public static ServiceResult<T> ShouldFailWith<T>(this ServiceResult<T> result, string expectedErrorCode)
    {
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue($"Expected ServiceResult<{typeof(T).Name}> to be Failure but was Success.");
        result.Error.Code.ShouldBe(expectedErrorCode);
        return result;
    }

    public static ServiceResult ShouldFailWithType(this ServiceResult result, ErrorType expectedType)
    {
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue("Expected ServiceResult to be Failure but was Success.");
        result.Error.Type.ShouldBe(expectedType);
        return result;
    }

    public static ServiceResult<T> ShouldFailWithType<T>(this ServiceResult<T> result, ErrorType expectedType)
    {
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue($"Expected ServiceResult<{typeof(T).Name}> to be Failure but was Success.");
        result.Error.Type.ShouldBe(expectedType);
        return result;
    }
}
