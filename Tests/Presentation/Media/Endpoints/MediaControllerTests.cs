using Application.Media.Features.Queries.GetEntityMedia;
using Application.Media.Features.Queries.GetMediaById;
using Application.Media.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Media.Endpoints;
using SharedKernel.Results;

namespace Tests.Presentation.Media.Endpoints;

public class MediaControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly MediaController _controller;

    public MediaControllerTests()
    {
        _controller = new MediaController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetMediaForEntity_WithValidInput_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        IReadOnlyList<MediaDto> expected =
            [new MediaDto { Id = Guid.NewGuid(), EntityType = "Product", EntityId = entityId }];

        _mediator.Send(Arg.Any<GetEntityMediaQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<MediaDto>>.Success(expected));

        // Act
        var result = await _controller.GetMediaForEntity("Product", entityId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<MediaDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Count.ShouldBe(1);
        await _mediator.Received(1).Send(
            Arg.Is<GetEntityMediaQuery>(q => q.EntityType == "Product" && q.EntityId == entityId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMediaById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new MediaDto { Id = id, FileName = "a.jpg" };

        _mediator.Send(Arg.Is<GetMediaByIdQuery>(q => q.MediaId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<MediaDto>.Success(expected));

        // Act
        var result = await _controller.GetMediaById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<MediaDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetMediaById_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetMediaByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<MediaDto>.NotFound());

        // Act
        var result = await _controller.GetMediaById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void MediaController_HasRouteAttribute()
    {
        var routeAttr = typeof(MediaController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/media");
    }

    [Theory]
    [InlineData(nameof(MediaController.GetMediaForEntity))]
    [InlineData(nameof(MediaController.GetMediaById))]
    public void Actions_AllowAnonymous(string methodName)
    {
        var method = typeof(MediaController).GetMethod(methodName);
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(nameof(MediaController.GetMediaForEntity), "{entityType}/{entityId}")]
    [InlineData(nameof(MediaController.GetMediaById), "{id:guid}")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(MediaController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}
