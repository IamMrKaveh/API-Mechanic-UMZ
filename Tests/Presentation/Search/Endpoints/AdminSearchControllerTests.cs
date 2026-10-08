using Application.Search.Features.Commands.RecreateSearchIndices;
using Application.Search.Features.Commands.SyncSearchData;
using Application.Search.Features.Queries.GetSearchIndexStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Search.Endpoints;
using SharedKernel.Results;

namespace Tests.Presentation.Search.Endpoints;

public class AdminSearchControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminSearchController _controller;

    public AdminSearchControllerTests()
    {
        _controller = new AdminSearchController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task SyncAllData_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<SyncSearchDataCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.SyncAllData(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<SyncSearchDataCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecreateIndices_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<RecreateSearchIndicesCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RecreateIndices(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<RecreateSearchIndicesCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIndexStats_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new SearchIndexStatsDto(100, 10, 20, 130);

        _mediator.Send(Arg.Any<GetSearchIndexStatsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<SearchIndexStatsDto>.Success(expected));

        // Act
        var result = await _controller.GetIndexStats(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<SearchIndexStatsDto>>();
        body.Data!.TotalDocuments.ShouldBe(130);
    }

    [Fact]
    public void AdminSearchController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminSearchController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminSearchController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminSearchController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/search");
    }

    [Theory]
    [InlineData(nameof(AdminSearchController.SyncAllData), "sync")]
    [InlineData(nameof(AdminSearchController.RecreateIndices), "indices")]
    [InlineData(nameof(AdminSearchController.GetIndexStats), "stats")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(AdminSearchController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
