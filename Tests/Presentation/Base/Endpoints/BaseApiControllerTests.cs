using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Endpoints.v1;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Results;

namespace Tests.Presentation.Base.Endpoints;

public class BaseApiControllerTests
{
    private sealed class TestableController : BaseApiController
    {
        public TestableController(IMediator mediator)
            : base(mediator)
        {
        }

        public TestableController(IMediator mediator, IMapper mapper)
            : base(mediator, mapper)
        {
        }

        public TestableController(IMediator mediator, IMapper mapper, IHttpResultMapper httpResultMapper)
            : base(mediator, mapper, httpResultMapper)
        {
        }

        public IActionResult CallToActionResult(ServiceResult result)
            => ToActionResult(result);

        public IActionResult CallToActionResult<T>(ServiceResult<T> result)
            => ToActionResult(result);

        public IActionResult CallToActionResultWithStatus(ServiceResult result, int statusCode)
            => ToActionResult(result, statusCode);

        public IActionResult CallToActionResultWithStatus<T>(ServiceResult<T> result, int statusCode)
            => ToActionResult(result, statusCode);

        public IActionResult CallToCreatedActionResult<T>(ServiceResult<T> result, string? location = null)
            => ToCreatedActionResult(result, location);

        public Task<IActionResult> CallSend(IRequest<ServiceResult> request, CancellationToken ct)
            => Send(request, ct);

        public Task<IActionResult> CallSend<T>(IRequest<ServiceResult<T>> request, CancellationToken ct)
            => Send<T>(request, ct);

        public Task<IActionResult> CallSendCreated<T>(IRequest<ServiceResult<T>> request, CancellationToken ct, string? location = null)
            => SendCreated<T>(request, ct, location);
    }

    private sealed record TestQuery : IRequest<ServiceResult<string>>;

    private sealed record TestCommand : IRequest<ServiceResult>;

    private static TestableController BuildController(
        IMediator? mediator = null,
        IMapper? mapper = null,
        IHttpResultMapper? httpResultMapper = null)
    {
        var controller = new TestableController(
            mediator ?? Substitute.For<IMediator>(),
            mapper ?? Substitute.For<IMapper>(),
            httpResultMapper ?? new HttpResultMapper());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        return controller;
    }

    [Fact]
    public void Constructor_WithMediator_StoresMediator()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();

        // Act
        var controller = new TestableController(mediator);

        // Assert
        controller.ShouldNotBeNull();
    }

    [Fact]
    public void ToActionResult_WithSuccess_ReturnsOk()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.CallToActionResult(ServiceResult.Success());

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public void ToActionResult_WithFailure_MapsStatusCode()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.CallToActionResult(ServiceResult.NotFound());

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToActionResultGeneric_WithSuccess_ReturnsOkWithData()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.CallToActionResult(ServiceResult<string>.Success("value"));

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<string>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldBe("value");
    }

    [Fact]
    public void ToActionResult_WithStatusCode_OverridesSuccessStatus()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.CallToActionResultWithStatus(
            ServiceResult.Success(), StatusCodes.Status204NoContent);

        // Assert
        // Note: success mapping produces OkObjectResult (derived from ObjectResult)
        // with the overridden status code.
        var obj = result.ShouldBeAssignableTo<ObjectResult>();
        obj.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void ToActionResult_WithStatusCode_DoesNotOverrideFailureStatus()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.CallToActionResultWithStatus(
            ServiceResult.NotFound(), StatusCodes.Status204NoContent);

        // Assert
        var obj = result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToCreatedActionResult_WithSuccess_Returns201()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.CallToCreatedActionResult(ServiceResult<string>.Success("value"));

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<string>>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public void ToCreatedActionResult_WithLocation_ReturnsCreatedResult()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.CallToCreatedActionResult(
            ServiceResult<string>.Success("value"), "/api/v1/items/1");

        // Assert
        var created = result.ShouldBeOfType<CreatedResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        created.Location.ShouldBe("/api/v1/items/1");
    }

    [Fact]
    public async Task Send_WithCommand_DelegatesToMediator_AndReturnsOk()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var controller = BuildController(mediator);
        mediator.Send(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await controller.CallSend(new TestCommand(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendGeneric_WithQuery_DelegatesToMediator_AndReturnsOk()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var controller = BuildController(mediator);
        mediator.Send(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<string>.Success("value"));

        // Act
        var result = await controller.CallSend(new TestQuery(), CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var body = ok.Value.ShouldBeOfType<ApiResponse<string>>();
        body.Data.ShouldBe("value");
    }

    [Fact]
    public async Task SendCreated_WithQuery_Returns201()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var controller = BuildController(mediator);
        mediator.Send(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<string>.Success("value"));

        // Act
        var result = await controller.CallSendCreated(new TestQuery(), CancellationToken.None);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
    }

    [Fact]
    public void BaseApiController_HasRouteAttribute()
    {
        var routeAttr = typeof(BaseApiController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/[controller]");
    }

    [Fact]
    public void BaseApiController_HasApiControllerAttribute()
    {
        typeof(BaseApiController).GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }
}
