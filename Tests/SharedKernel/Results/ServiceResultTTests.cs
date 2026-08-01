using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.SharedKernel.Results;

public class ServiceResultTTests
{
    [Fact]
    public void Success_WithValue_ReturnsSuccessCarryingValue()
    {
        var sut = ServiceResult<int>.Success(42);

        sut.ShouldBeSuccess();
        sut.Value.ShouldBe(42);
        sut.ValueOrDefault.ShouldBe(42);
    }

    [Fact]
    public void Success_WithReferenceValue_AllowsAccessThroughValue()
    {
        var payload = new { Name = "x" };

        var sut = ServiceResult<object>.Success(payload);

        sut.Value.ShouldBeSameAs(payload);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        var sut = ServiceResult<int>.Failure(Error.NotFound("nf"));

        Should.Throw<InvalidOperationException>(() => sut.Value);
    }

    [Fact]
    public void ValueOrDefault_OnFailure_ReturnsDefaultOfT()
    {
        var sut = ServiceResult<int>.Failure(Error.NotFound("nf"));

        sut.ValueOrDefault.ShouldBe(0);
    }

    [Fact]
    public void ValueOrDefault_OnFailureForReferenceType_ReturnsNull()
    {
        var sut = ServiceResult<string>.Failure(Error.NotFound("nf"));

        sut.ValueOrDefault.ShouldBeNull();
    }

    [Fact]
    public void ImplicitFromValue_ConvertsValueIntoSuccessResult()
    {
        ServiceResult<int> sut = 7;

        sut.ShouldBeSuccess();
        sut.Value.ShouldBe(7);
    }

    [Fact]
    public void ImplicitFromError_ConvertsErrorIntoFailureResult()
    {
        ServiceResult<int> sut = Error.Conflict("dup");

        sut.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public void ImplicitToBool_OnSuccess_ReturnsTrue()
    {
        ((bool)ServiceResult<int>.Success(1)).ShouldBeTrue();
    }

    [Fact]
    public void ImplicitToBool_OnFailure_ReturnsFalse()
    {
        ((bool)ServiceResult<int>.Failure(Error.Failure("x"))).ShouldBeFalse();
    }

    [Fact]
    public void Map_OnSuccess_TransformsValueAndKeepsSuccess()
    {
        var sut = ServiceResult<int>.Success(5).Map(x => x * 2);

        sut.ShouldBeSuccess();
        sut.Value.ShouldBe(10);
    }

    [Fact]
    public void Map_OnFailure_PropagatesFailureWithoutInvokingMapper()
    {
        var mapperCalled = false;

        var sut = ServiceResult<int>.Failure(Error.NotFound("nf"))
                                    .Map(x => { mapperCalled = true; return x * 2; });

        mapperCalled.ShouldBeFalse();
        sut.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public void Bind_OnSuccess_InvokesBinderWithValue()
    {
        var sut = ServiceResult<int>.Success(5)
                                    .Bind(x => ServiceResult<string>.Success($"v{x}"));

        sut.ShouldBeSuccess();
        sut.Value.ShouldBe("v5");
    }

    [Fact]
    public void Bind_OnSuccessReturningFailure_ReturnsThatFailure()
    {
        var sut = ServiceResult<int>.Success(5)
                                    .Bind(_ => ServiceResult<string>.Conflict("dup"));

        sut.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public void Bind_OnFailure_PropagatesFailureWithoutInvokingBinder()
    {
        var binderCalled = false;

        var sut = ServiceResult<int>.Failure(Error.NotFound("nf"))
                                    .Bind(x => { binderCalled = true; return ServiceResult<string>.Success("x"); });

        binderCalled.ShouldBeFalse();
        sut.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public void Tap_OnSuccess_ExecutesActionWithValue()
    {
        int? captured = null;

        var sut = ServiceResult<int>.Success(9).Tap(x => captured = x);

        captured.ShouldBe(9);
        sut.ShouldBeSuccess();
    }

    [Fact]
    public void Tap_OnFailure_DoesNotExecuteAction()
    {
        var called = false;

        ServiceResult<int>.Failure(Error.Failure("x")).Tap(_ => called = true);

        called.ShouldBeFalse();
    }

    [Fact]
    public void Ensure_OnSuccessAndPredicateTrue_ReturnsSameResult()
    {
        var sut = ServiceResult<int>.Success(10).Ensure(x => x > 0, Error.Validation("neg"));

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void Ensure_OnSuccessAndPredicateFalse_ReturnsFailureWithProvidedError()
    {
        var sut = ServiceResult<int>.Success(-1).Ensure(x => x > 0, Error.Validation("neg"));

        sut.ShouldFailWith(ErrorCode.Validation);
        sut.Error.Message.ShouldBe("neg");
    }

    [Fact]
    public void Ensure_OnFailure_PropagatesOriginalFailureWithoutInvokingPredicate()
    {
        var called = false;

        var sut = ServiceResult<int>.Failure(Error.NotFound("nf"))
                                    .Ensure(x => { called = true; return true; }, Error.Conflict("c"));

        called.ShouldBeFalse();
        sut.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public void Match_OnSuccess_InvokesOnSuccessBranchWithValue()
    {
        var value = ServiceResult<int>.Success(7).Match(x => x + 1, _ => -1);

        value.ShouldBe(8);
    }

    [Fact]
    public void Match_OnFailure_InvokesOnFailureBranchWithError()
    {
        Error? captured = null;

        var value = ServiceResult<int>.Failure(Error.NotFound("nf"))
                                      .Match(_ => 0, e => { captured = e; return -1; });

        value.ShouldBe(-1);
        captured!.Code.ShouldBe(ErrorCode.NotFound);
    }

    [Theory]
    [InlineData(ErrorType.Validation, ErrorCode.Validation)]
    [InlineData(ErrorType.NotFound, ErrorCode.NotFound)]
    [InlineData(ErrorType.Conflict, ErrorCode.Conflict)]
    [InlineData(ErrorType.Unauthorized, ErrorCode.Unauthorized)]
    [InlineData(ErrorType.Forbidden, ErrorCode.Forbidden)]
    [InlineData(ErrorType.RateLimitExceeded, ErrorCode.RateLimitExceeded)]
    [InlineData(ErrorType.BusinessRule, ErrorCode.BusinessRule)]
    [InlineData(ErrorType.Infrastructure, ErrorCode.Infrastructure)]
    [InlineData(ErrorType.ExternalService, ErrorCode.ExternalService)]
    [InlineData(ErrorType.Unexpected, ErrorCode.Unexpected)]
    public void FailureWithType_MapsExpectedCode(ErrorType type, string expectedCode)
    {
        ServiceResult<int>.Failure("m", type).ShouldFailWith(expectedCode);
    }
}
