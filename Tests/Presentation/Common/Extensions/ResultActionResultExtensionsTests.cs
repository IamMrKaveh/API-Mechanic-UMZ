using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;
using Presentation.Common.Interfaces;

namespace Tests.Presentation.Common.Extensions;

public class ResultActionResultExtensionsTests
{
    private readonly IHttpResultMapper _mapper = Substitute.For<IHttpResultMapper>();

    [Fact]
    public void ToActionResult_NonGeneric_DelegatesToMapper()
    {
        var result = ServiceResult.Success();
        var expected = new OkObjectResult("ok");
        _mapper.Map(result).Returns(expected);

        var actual = result.ToActionResult(_mapper);

        actual.ShouldBeSameAs(expected);
        _mapper.Received(1).Map(result);
    }

    [Fact]
    public void ToActionResult_Generic_DelegatesToMapper()
    {
        var result = ServiceResult<int>.Success(5);
        var expected = new OkObjectResult(5);
        _mapper.Map(result).Returns(expected);

        var actual = result.ToActionResult(_mapper);

        actual.ShouldBeSameAs(expected);
        _mapper.Received(1).Map(result);
    }

    [Fact]
    public void ToCreatedActionResult_PassesThroughLocation()
    {
        var result = ServiceResult<int>.Success(5);
        var expected = new CreatedResult("/x", 5);
        _mapper.MapCreated(result, "/x").Returns(expected);

        var actual = result.ToCreatedActionResult(_mapper, "/x");

        actual.ShouldBeSameAs(expected);
        _mapper.Received(1).MapCreated(result, "/x");
    }

    [Fact]
    public void ToCreatedActionResult_DefaultLocation_IsNull()
    {
        var result = ServiceResult<int>.Success(5);
        var expected = new OkObjectResult(5);
        _mapper.MapCreated(result, null).Returns(expected);

        var actual = result.ToCreatedActionResult(_mapper);

        actual.ShouldBeSameAs(expected);
        _mapper.Received(1).MapCreated(result, null);
    }
}
