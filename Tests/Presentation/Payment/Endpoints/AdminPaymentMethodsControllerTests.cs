using Application.Payment.Features.Commands.ActivatePaymentMethod;
using Application.Payment.Features.Commands.CreatePaymentMethod;
using Application.Payment.Features.Commands.DeactivatePaymentMethod;
using Application.Payment.Features.Commands.DeletePaymentMethod;
using Application.Payment.Features.Commands.UpdatePaymentMethod;
using Application.Payment.Features.Queries.GetPaymentMethod;
using Application.Payment.Features.Queries.GetPaymentMethods;
using Application.Payment.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Payment.Endpoints;
using Presentation.Payment.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.Payment.Endpoints;

public class AdminPaymentMethodsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminPaymentMethodsController _controller;

    public AdminPaymentMethodsControllerTests()
    {
        _controller = new AdminPaymentMethodsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetPaymentMethods_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<PaymentMethodListItemDto> expected =
            [new PaymentMethodListItemDto { Id = Guid.NewGuid(), Name = "ZarinPal", Code = "zarinpal" }];

        _mediator.Send(Arg.Any<GetPaymentMethodsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<PaymentMethodListItemDto>>.Success(expected));

        // Act
        var result = await _controller.GetPaymentMethods();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<PaymentMethodListItemDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Count.ShouldBe(1);
        await _mediator.Received(1).Send(
            Arg.Is<GetPaymentMethodsQuery>(q => q.IncludeInactive && !q.IncludeDeleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaymentMethodById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new PaymentMethodDto { Id = id, Name = "ZarinPal", Code = "zarinpal" };

        _mediator.Send(Arg.Is<GetPaymentMethodQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentMethodDto>.Success(expected));

        // Act
        var result = await _controller.GetPaymentMethodById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaymentMethodDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task CreatePaymentMethod_MapsRequestToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new CreatePaymentMethodRequest("ZarinPal", "zarinpal", null, null, 0, 0, 1);
        var command = new CreatePaymentMethodCommand("ZarinPal", "zarinpal", null, null, 0, 0, 1);
        var expected = new PaymentMethodDto { Id = Guid.NewGuid(), Name = "ZarinPal", Code = "zarinpal" };

        _mapper.Map<CreatePaymentMethodCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<CreatePaymentMethodCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentMethodDto>.Success(expected));

        // Act
        var result = await _controller.CreatePaymentMethod(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaymentMethodDto>>();
        body.Data!.Code.ShouldBe("zarinpal");
    }

    [Fact]
    public async Task UpdatePaymentMethod_MapsRequestWithRouteId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdatePaymentMethodRequest("ZarinPal", null, null, 0, 0, 2);
        var mapped = new UpdatePaymentMethodCommand(Guid.Empty, "ZarinPal", null, null, 0, 0, 2);
        var expected = new PaymentMethodDto { Id = id, Name = "ZarinPal" };

        _mapper.Map<UpdatePaymentMethodCommand>(request).Returns(mapped);
        _mediator.Send(Arg.Any<UpdatePaymentMethodCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentMethodDto>.Success(expected));

        // Act
        var result = await _controller.UpdatePaymentMethod(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdatePaymentMethodCommand>(c => c.Id == id && c.Name == "ZarinPal" && c.SortOrder == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivatePaymentMethod_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<ActivatePaymentMethodCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ActivatePaymentMethod(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ActivatePaymentMethodCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivatePaymentMethod_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeactivatePaymentMethodCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeactivatePaymentMethod(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeactivatePaymentMethodCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePaymentMethod_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeletePaymentMethodCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeletePaymentMethod(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeletePaymentMethodCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaymentMethodById_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetPaymentMethodQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentMethodDto>.NotFound());

        // Act
        var result = await _controller.GetPaymentMethodById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminPaymentMethodsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminPaymentMethodsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminPaymentMethodsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminPaymentMethodsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/payment-methods");
    }

    [Theory]
    [InlineData(nameof(AdminPaymentMethodsController.GetPaymentMethods), null)]
    [InlineData(nameof(AdminPaymentMethodsController.GetPaymentMethodById), "{id:guid}")]
    [InlineData(nameof(AdminPaymentMethodsController.CreatePaymentMethod), null)]
    [InlineData(nameof(AdminPaymentMethodsController.UpdatePaymentMethod), "{id:guid}")]
    [InlineData(nameof(AdminPaymentMethodsController.ActivatePaymentMethod), "{id:guid}/activate")]
    [InlineData(nameof(AdminPaymentMethodsController.DeactivatePaymentMethod), "{id:guid}/deactivate")]
    [InlineData(nameof(AdminPaymentMethodsController.DeletePaymentMethod), "{id:guid}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminPaymentMethodsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
