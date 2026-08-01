using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.SharedKernel.Results;

public class ServiceResultTests
{
    [Fact]
    public void Success_WithoutArguments_ReturnsSuccessResultCarryingErrorNone()
    {
        var sut = ServiceResult.Success();

        sut.ShouldBeSuccess();
        sut.IsFailure.ShouldBeFalse();
        sut.IsFailed.ShouldBeFalse();
    }

    [Fact]
    public void Failure_WithError_ReturnsFailureResultCarryingThatError()
    {
        var err = Error.Validation("bad");

        var sut = ServiceResult.Failure(err);

        sut.ShouldFailWith(ErrorCode.Validation);
        sut.Error.Message.ShouldBe("bad");
        sut.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Failure_WithMessageAndDefaultType_UsesFailureCodeAndFailureType()
    {
        var sut = ServiceResult.Failure("boom");

        sut.ShouldFailWith(ErrorCode.Failure);
        sut.Error.Message.ShouldBe("boom");
        sut.Error.Type.ShouldBe(ErrorType.Failure);
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
    public void Failure_WithMessageAndExplicitType_MapsCorrectCode(ErrorType type, string expectedCode)
    {
        var sut = ServiceResult.Failure("m", type);

        sut.ShouldFailWith(expectedCode);
        sut.Error.Type.ShouldBe(type);
    }

    [Fact]
    public void Validation_WithMessage_ProducesValidationFailure()
    {
        ServiceResult.Validation("bad").ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public void Validation_WithErrorList_AttachesValidationErrorsAndUsesDefaultMessage()
    {
        IReadOnlyList<ValidationError> errors =
        [
            new("Email", "invalid"),
            new("Phone", "invalid")
        ];

        var sut = ServiceResult.Validation(errors);

        sut.ShouldFailWith(ErrorCode.Validation);
        sut.Error.Message.ShouldBe("اطلاعات ورودی نامعتبر است.");
        sut.Error.ValidationErrors.ShouldNotBeNull();
        sut.Error.ValidationErrors!.Count.ShouldBe(2);
    }

    [Fact]
    public void NotFound_WithDefaultMessage_UsesPersianDefault()
    {
        var sut = ServiceResult.NotFound();

        sut.ShouldFailWith(ErrorCode.NotFound);
        sut.Error.Message.ShouldBe("یافت نشد.");
    }

    [Fact]
    public void Conflict_WithMessage_ProducesConflictFailure()
    {
        ServiceResult.Conflict("dup").ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public void BusinessRule_WithCodeAndMessage_KeepsCustomCodeAndBusinessRuleType()
    {
        var sut = ServiceResult.BusinessRule("ORDER_MIN", "min");

        sut.ShouldFailWith("ORDER_MIN");
        sut.Error.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void Constructor_SuccessCarryingNonNoneError_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(() => ServiceResult.Failure(Error.None));
    }

    [Fact]
    public void ImplicitFromError_ConvertsErrorToFailure()
    {
        ServiceResult sut = Error.Conflict("dup");

        sut.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public void ImplicitToBool_OnSuccess_ReturnsTrue()
    {
        ((bool)ServiceResult.Success()).ShouldBeTrue();
    }

    [Fact]
    public void ImplicitToBool_OnFailure_ReturnsFalse()
    {
        ((bool)ServiceResult.Failure(Error.Failure("x"))).ShouldBeFalse();
    }

    [Fact]
    public void Tap_OnSuccess_ExecutesAction()
    {
        var executed = false;

        var sut = ServiceResult.Success().Tap(() => executed = true);

        executed.ShouldBeTrue();
        sut.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Tap_OnFailure_DoesNotExecuteAction()
    {
        var executed = false;

        var sut = ServiceResult.Failure(Error.Failure("x")).Tap(() => executed = true);

        executed.ShouldBeFalse();
        sut.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Ensure_OnSuccessAndPredicateTrue_ReturnsSameResult()
    {
        var sut = ServiceResult.Success().Ensure(() => true, Error.Conflict("c"));

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void Ensure_OnSuccessAndPredicateFalse_ReturnsFailureWithGivenError()
    {
        var sut = ServiceResult.Success().Ensure(() => false, Error.Conflict("blocked"));

        sut.ShouldFailWith(ErrorCode.Conflict);
        sut.Error.Message.ShouldBe("blocked");
    }

    [Fact]
    public void Ensure_OnFailure_ShortCircuitsWithoutEvaluatingPredicate()
    {
        var predicateCalled = false;

        var sut = ServiceResult.Failure(Error.NotFound("nf"))
                               .Ensure(() => { predicateCalled = true; return true; }, Error.Conflict("c"));

        predicateCalled.ShouldBeFalse();
        sut.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public void Match_OnSuccess_InvokesOnSuccessBranch()
    {
        var value = ServiceResult.Success().Match(() => 1, _ => 2);

        value.ShouldBe(1);
    }

    [Fact]
    public void Match_OnFailure_InvokesOnFailureBranchAndReceivesTheError()
    {
        Error? captured = null;

        var value = ServiceResult.Failure(Error.NotFound("nf"))
                                 .Match(() => 1, e => { captured = e; return 2; });

        value.ShouldBe(2);
        captured.ShouldNotBeNull();
        captured!.Code.ShouldBe(ErrorCode.NotFound);
    }

    [Theory]
    [InlineData(ErrorType.Validation, ResultType.BadRequest)]
    [InlineData(ErrorType.NotFound, ResultType.NotFound)]
    [InlineData(ErrorType.Conflict, ResultType.Conflict)]
    [InlineData(ErrorType.Unauthorized, ResultType.Unauthorized)]
    [InlineData(ErrorType.Forbidden, ResultType.Forbidden)]
    [InlineData(ErrorType.RateLimitExceeded, ResultType.RateLimitExceeded)]
    [InlineData(ErrorType.BusinessRule, ResultType.Unexpected)]
    [InlineData(ErrorType.Infrastructure, ResultType.Unexpected)]
    [InlineData(ErrorType.ExternalService, ResultType.Unexpected)]
    [InlineData(ErrorType.Unexpected, ResultType.Unexpected)]
    [InlineData(ErrorType.Failure, ResultType.Unexpected)]
    public void ToResultType_OnFailure_MapsErrorTypeToResultType(ErrorType errorType, ResultType expected)
    {
        var sut = ServiceResult.Failure("m", errorType);

        sut.ToResultType().ShouldBe(expected);
    }

    [Fact]
    public void ToResultType_OnSuccess_ReturnsOk()
    {
        ServiceResult.Success().ToResultType().ShouldBe(ResultType.Ok);
    }
}
