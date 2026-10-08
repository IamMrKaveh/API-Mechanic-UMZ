using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Payment.Endpoints;

namespace Tests.Presentation.Payment.Endpoints;

public class MockGatewayControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private MockGatewayController BuildController(string environmentName)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(environmentName);

        var controller = new MockGatewayController(env, _mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        return controller;
    }

    [Fact]
    public void Index_InDevelopment_ReturnsHtmlContent()
    {
        // Arrange
        var controller = BuildController(Environments.Development);

        // Act
        var result = controller.Index("order-1", 1000);

        // Assert
        // Note: Content(html, "text/html") leaves StatusCode null; the framework
        // sends 200 at execution time, so accept the default (null => 200).
        var content = result.ShouldBeOfType<ContentResult>();
        (content.StatusCode ?? StatusCodes.Status200OK).ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldBe("text/html");
        content.Content.ShouldContain("Mock Payment Gateway");
        content.Content.ShouldContain("order-1");
        content.Content.ShouldContain("1000");
    }

    [Fact]
    public void Index_InProduction_ReturnsNotFound()
    {
        // Arrange
        var controller = BuildController(Environments.Production);

        // Act
        var result = controller.Index("order-1", 1000);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public void MockGatewayController_HasRouteAttribute()
    {
        var routeAttr = typeof(MockGatewayController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/mock-gateway");
    }

    [Fact]
    public void Index_HasHttpGetWithNullTemplate()
    {
        var method = typeof(MockGatewayController).GetMethod(nameof(MockGatewayController.Index));
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBeNull();
    }
}
