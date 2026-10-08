using Application.User.Features.Queries.GetUserDashboard;
using Application.User.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.User.Endpoints;
using SharedKernel.Results;

namespace Tests.Presentation.User.Endpoints;

public class DashboardControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _controller = new DashboardController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetDashboardSummary_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new UserDashboardDto
        {
            UserProfile = new UserProfileDto { Id = Guid.NewGuid(), PhoneNumber = "09123456789" },
            TotalOrders = 5,
            TotalSpent = 100000
        };

        _mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserDashboardDto>.Success(expected));

        // Act
        var result = await _controller.GetDashboardSummary(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<UserDashboardDto>>();
        body.Data!.TotalOrders.ShouldBe(5);
        await _mediator.Received(1).Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DashboardController_HasAuthorizeAttribute()
    {
        typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void DashboardController_HasRouteAttribute()
    {
        var routeAttr = typeof(DashboardController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/dashboard");
    }
}
