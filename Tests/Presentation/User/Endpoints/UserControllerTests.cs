using Application.User.Features.Commands.UpdateUser;
using Application.User.Features.Queries.GetUserById;
using Application.User.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.User.Endpoints;
using Presentation.User.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.User.Endpoints;

public class UserControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _controller = new UserController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetUser_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new UserProfileDto { Id = id, PhoneNumber = "09123456789" };

        _mediator.Send(Arg.Is<GetUserByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserProfileDto?>.Success(expected));

        // Act
        var result = await _controller.GetUser(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<UserProfileDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task UpdateUser_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateProfileRequest("Ali", "Rezaei");

        _mediator.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateUser(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateUserCommand>(c => c.Id == id && c.FirstName == "Ali"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UserController_HasAuthorizeAttribute()
    {
        typeof(UserController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void UserController_HasRouteAttribute()
    {
        var routeAttr = typeof(UserController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/users");
    }

    [Theory]
    [InlineData(nameof(UserController.GetUser), "{id:guid}")]
    [InlineData(nameof(UserController.UpdateUser), "{id:guid}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(UserController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
