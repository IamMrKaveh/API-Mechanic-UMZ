using SharedKernel.Results;

namespace Tests.SharedKernel.Results;

public class ErrorTests
{
    [Fact]
    public void None_IsSingletonSentinel_WithEmptyCodeAndMessageAndFailureType()
    {
        Error.None.Code.ShouldBe(string.Empty);
        Error.None.Message.ShouldBe(string.Empty);
        Error.None.Type.ShouldBe(ErrorType.Failure);
    }

    [Fact]
    public void Failure_WithMessageOnly_UsesGenericFailureCodeAndFailureType()
    {
        var sut = Error.Failure("something broke");

        sut.Code.ShouldBe(ErrorCode.Failure);
        sut.Message.ShouldBe("something broke");
        sut.Type.ShouldBe(ErrorType.Failure);
    }

    [Fact]
    public void Failure_WithCustomCodeAndMessage_KeepsCustomCodeAndFailureType()
    {
        var sut = Error.Failure("MY_CODE", "custom failure");

        sut.Code.ShouldBe("MY_CODE");
        sut.Message.ShouldBe("custom failure");
        sut.Type.ShouldBe(ErrorType.Failure);
    }

    [Theory]
    [InlineData(nameof(ErrorType.Validation), ErrorCode.Validation, ErrorType.Validation)]
    [InlineData(nameof(ErrorType.NotFound), ErrorCode.NotFound, ErrorType.NotFound)]
    [InlineData(nameof(ErrorType.Conflict), ErrorCode.Conflict, ErrorType.Conflict)]
    [InlineData(nameof(ErrorType.Forbidden), ErrorCode.Forbidden, ErrorType.Forbidden)]
    [InlineData(nameof(ErrorType.Unauthorized), ErrorCode.Unauthorized, ErrorType.Unauthorized)]
    [InlineData(nameof(ErrorType.RateLimitExceeded), ErrorCode.RateLimitExceeded, ErrorType.RateLimitExceeded)]
    [InlineData(nameof(ErrorType.Infrastructure), ErrorCode.Infrastructure, ErrorType.Infrastructure)]
    [InlineData(nameof(ErrorType.ExternalService), ErrorCode.ExternalService, ErrorType.ExternalService)]
    [InlineData(nameof(ErrorType.Unexpected), ErrorCode.Unexpected, ErrorType.Unexpected)]
    public void SingleArgFactory_ForEachType_UsesDefaultCodeAndCorrectType(
        string factory, string expectedCode, ErrorType expectedType)
    {
        Error sut = factory switch
        {
            nameof(ErrorType.Validation) => Error.Validation("m"),
            nameof(ErrorType.NotFound) => Error.NotFound("m"),
            nameof(ErrorType.Conflict) => Error.Conflict("m"),
            nameof(ErrorType.Forbidden) => Error.Forbidden("m"),
            nameof(ErrorType.Unauthorized) => Error.Unauthorized("m"),
            nameof(ErrorType.RateLimitExceeded) => Error.RateLimitExceeded("m"),
            nameof(ErrorType.Infrastructure) => Error.Infrastructure("m"),
            nameof(ErrorType.ExternalService) => Error.ExternalService("m"),
            nameof(ErrorType.Unexpected) => Error.Unexpected("m"),
            _ => Error.Failure("m")
        };

        sut.Code.ShouldBe(expectedCode);
        sut.Message.ShouldBe("m");
        sut.Type.ShouldBe(expectedType);
    }

    [Fact]
    public void Validation_WithCustomCode_KeepsCustomCodeAndValidationType()
    {
        var sut = Error.Validation("EMAIL_INVALID", "Email is not valid.");

        sut.Code.ShouldBe("EMAIL_INVALID");
        sut.Message.ShouldBe("Email is not valid.");
        sut.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void BusinessRule_WithCodeAndMessage_TypedAsBusinessRule()
    {
        var sut = Error.BusinessRule("ORDER_MIN_TOTAL", "Order total must be at least 100.");

        sut.Code.ShouldBe("ORDER_MIN_TOTAL");
        sut.Message.ShouldBe("Order total must be at least 100.");
        sut.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void WithMetadata_ReturnsNewErrorInstanceWithMetadataAttached()
    {
        var original = Error.Validation("v");
        var metadata = new Dictionary<string, object?> { ["Field"] = "Email" };

        var enriched = original.WithMetadata(metadata);

        enriched.ShouldNotBeSameAs(original);
        enriched.Metadata.ShouldNotBeNull();
        enriched.Metadata!["Field"].ShouldBe("Email");
        original.Metadata.ShouldBeNull();
    }

    [Fact]
    public void WithValidationErrors_ReturnsNewErrorInstanceWithValidationErrorsAttached()
    {
        var original = Error.Validation("v");
        IReadOnlyList<ValidationError> errors =
        [
            new("Email", "invalid"),
            new("Phone", "invalid")
        ];

        var enriched = original.WithValidationErrors(errors);

        enriched.ValidationErrors.ShouldNotBeNull();
        enriched.ValidationErrors!.Count.ShouldBe(2);
        enriched.ValidationErrors[0].Property.ShouldBe("Email");
        original.ValidationErrors.ShouldBeNull();
    }

    [Fact]
    public void WithInnerErrors_ReturnsNewErrorInstanceWithInnerErrorsAttached()
    {
        var original = Error.Failure("outer");
        IReadOnlyList<Error> inner = [Error.Conflict("A"), Error.NotFound("B")];

        var enriched = original.WithInnerErrors(inner);

        enriched.InnerErrors.ShouldNotBeNull();
        enriched.InnerErrors!.Count.ShouldBe(2);
        enriched.InnerErrors[0].Type.ShouldBe(ErrorType.Conflict);
        enriched.InnerErrors[1].Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void Equality_ForRecordWithSameCoreMembers_TreatsInstancesAsEqual()
    {
        var a = new Error("C", "m", ErrorType.Conflict);
        var b = new Error("C", "m", ErrorType.Conflict);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_ForErrorNone_TreatsSingletonInstanceAsEqualToItself()
    {
        var copy = Error.None with { };

        Error.None.ShouldBe(copy);
        (Error.None == copy).ShouldBeTrue();
    }
}
