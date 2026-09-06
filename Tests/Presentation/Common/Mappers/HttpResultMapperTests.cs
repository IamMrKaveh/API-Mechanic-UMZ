using Microsoft.AspNetCore.Mvc;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;

namespace Tests.Presentation.Common.Mappers;

public class HttpResultMapperTests
{
    private readonly HttpResultMapper _sut = new();

    [Fact]
    public void Map_Success_ReturnsOkWithSuccessResponse()
    {
        var result = _sut.Map(ServiceResult.Success());

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var body = ok.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.RateLimitExceeded, 429)]
    [InlineData(ErrorType.BusinessRule, 422)]
    [InlineData(ErrorType.Failure, 500)]
    [InlineData(ErrorType.Infrastructure, 500)]
    [InlineData(ErrorType.ExternalService, 500)]
    [InlineData(ErrorType.Unexpected, 500)]
    public void Map_Failure_ReturnsExpectedStatusCode(ErrorType type, int expectedStatus)
    {
        var result = _sut.Map(ServiceResult.Failure("boom", type));

        var obj = result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(expectedStatus);
        var body = obj.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeFalse();
        body.Message.ShouldBe("boom");
    }

    [Fact]
    public void Map_GenericSuccess_ReturnsOkWithValue()
    {
        var result = _sut.Map(ServiceResult<string>.Success("hello"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var body = ok.Value.ShouldBeOfType<ApiResponse<string>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldBe("hello");
    }

    [Fact]
    public void Map_GenericFailure_ReturnsExpectedStatusAndEmptyData()
    {
        var result = _sut.Map(ServiceResult<string>.NotFound("missing"));

        var obj = result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(404);
        var body = obj.Value.ShouldBeOfType<ApiResponse<string>>();
        body.Success.ShouldBeFalse();
        body.Data.ShouldBeNull();
        body.Message.ShouldBe("missing");
    }

    [Fact]
    public void Map_FailureWithValidationErrors_GroupsByProperty()
    {
        var error = Error.Validation("invalid").WithValidationErrors(new List<ValidationError>
        {
            new("Name", "required"),
            new("Name", "too short"),
            new("", "global issue")
        });

        var result = _sut.Map(ServiceResult.Failure(error));

        var obj = result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(400);
        var body = obj.Value.ShouldBeOfType<ApiResponse>();
        body.Errors.ShouldNotBeNull();
        body.Errors!["Name"].ShouldBe(["required", "too short"]);
        body.Errors["domain"].ShouldBe(["global issue"]);
    }

    [Fact]
    public void Map_FailureWithCode_UsesCodeAsErrorKey()
    {
        var result = _sut.Map(ServiceResult.Failure(new Error("USER_NOT_FOUND", "nope", ErrorType.NotFound)));

        var body = result.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ApiResponse>();
        body.Errors!["USER_NOT_FOUND"].ShouldBe(["nope"]);
    }

    [Fact]
    public void Map_FailureWithBlankCode_UsesDomainAsErrorKey()
    {
        var result = _sut.Map(ServiceResult.Failure(new Error("", "nope", ErrorType.Failure)));

        var body = result.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ApiResponse>();
        body.Errors!["domain"].ShouldBe(["nope"]);
    }

    [Fact]
    public void Map_FailureWithEmptyMessage_ReturnsEmptyErrors()
    {
        var result = _sut.Map(ServiceResult.Failure(new Error("CODE", "", ErrorType.Failure)));

        var body = result.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ApiResponse>();
        body.Errors.ShouldNotBeNull();
        body.Errors!.Count.ShouldBe(0);
    }

    [Fact]
    public void MapCreated_SuccessWithoutLocation_Returns201ObjectResult()
    {
        var result = _sut.MapCreated(ServiceResult<int>.Success(42));

        var obj = result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(201);
        var body = obj.Value.ShouldBeOfType<ApiResponse<int>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldBe(42);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MapCreated_SuccessWithBlankLocation_Returns201ObjectResult(string? location)
    {
        var result = _sut.MapCreated(ServiceResult<int>.Success(1), location);

        result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(201);
    }

    [Fact]
    public void MapCreated_SuccessWithLocation_ReturnsCreatedResult()
    {
        var result = _sut.MapCreated(ServiceResult<int>.Success(7), "/api/items/7");

        var created = result.ShouldBeOfType<CreatedResult>();
        created.Location.ShouldBe("/api/items/7");
        created.StatusCode.ShouldBe(201);
        var body = created.Value.ShouldBeOfType<ApiResponse<int>>();
        body.Data.ShouldBe(7);
    }

    [Fact]
    public void MapCreated_Failure_DelegatesToMap()
    {
        var result = _sut.MapCreated(ServiceResult<int>.NotFound("gone"), "/api/items/1");

        var obj = result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(404);
    }
}
