using Application.Payment.Features.Commands.InitiatePayment;
using Application.Payment.Features.Commands.ProcessWebhook;
using Application.Payment.Features.Commands.VerifyPayment;
using Application.Payment.Features.Queries.GetPaymentByAuthority;
using Application.Payment.Features.Queries.GetPaymentsByOrder;
using Application.Payment.Features.Queries.GetPaymentStatus;
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

public class PaymentsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _controller = new PaymentsController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task VerifyPayment_WithValidInput_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var expected = new PaymentVerificationResult(Guid.NewGuid(), true, 12345, null, 0);

        _mediator.Send(Arg.Any<VerifyPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentVerificationResult>.Success(expected));

        // Act
        var result = await _controller.VerifyPayment("AUTH123", "OK", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaymentVerificationResult>>();
        body.Success.ShouldBeTrue();
        body.Data!.IsVerified.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Is<VerifyPaymentCommand>(c => c.Authority == "AUTH123" && c.Status == "OK"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAuthority_WithValidAuthority_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new PaymentTransactionDto { Id = Guid.NewGuid(), Authority = "AUTH123", Status = "Paid" };

        _mediator.Send(Arg.Is<GetPaymentByAuthorityQuery>(q => q.Authority == "AUTH123"), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentTransactionDto?>.Success(expected));

        // Act
        var result = await _controller.GetByAuthority("AUTH123", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaymentTransactionDto>>();
        body.Data!.Authority.ShouldBe("AUTH123");
    }

    [Fact]
    public async Task GetPaymentsByOrder_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        IEnumerable<PaymentTransactionDto> expected =
            [new PaymentTransactionDto { Id = Guid.NewGuid(), OrderId = orderId }];

        _mediator.Send(Arg.Is<GetPaymentsByOrderQuery>(q => q.OrderId == orderId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IEnumerable<PaymentTransactionDto>>.Success(expected));

        // Act
        var result = await _controller.GetPaymentsByOrder(orderId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IEnumerable<PaymentTransactionDto>>>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPaymentStatus_WithValidAuthority_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new PaymentStatusDto { Authority = "AUTH123", Status = "Paid", IsSuccess = true };

        _mediator.Send(Arg.Is<GetPaymentStatusQuery>(q => q.Authority == "AUTH123"), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentStatusDto?>.Success(expected));

        // Act
        var result = await _controller.GetPaymentStatus("AUTH123", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaymentStatusDto>>();
        body.Data!.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task InitiatePayment_WithValidRequest_SendsCommand_AndReturnsCreated()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new InitiatePaymentRequest(orderId, "ZarinPal");
        var expected = new PaymentInitiationResult("AUTH123", "https://pay.example/AUTH123", Guid.NewGuid());

        _mediator.Send(Arg.Any<InitiatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentInitiationResult>.Success(expected));

        // Act
        var result = await _controller.InitiatePayment(request, CancellationToken.None);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<PaymentInitiationResult>>();
        body.Data!.Authority.ShouldBe("AUTH123");
        await _mediator.Received(1).Send(
            Arg.Is<InitiatePaymentCommand>(c => c.OrderId == orderId && c.GatewayName == "ZarinPal"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Webhook_WithValidPayload_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var payload = new WebhookPayloadRequest("AUTH123", "OK", "nonce-1");

        _mediator.Send(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Webhook(payload, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ProcessWebhookCommand>(c => c.Authority == "AUTH123" && c.Status == "OK" && c.Nonce == "nonce-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAuthority_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        _mediator.Send(Arg.Any<GetPaymentByAuthorityQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaymentTransactionDto?>.NotFound());

        // Act
        var result = await _controller.GetByAuthority("MISSING", CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void PaymentsController_HasRouteAttribute()
    {
        var routeAttr = typeof(PaymentsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/payments");
    }

    [Theory]
    [InlineData(nameof(PaymentsController.VerifyPayment), "verify")]
    [InlineData(nameof(PaymentsController.GetByAuthority), "{authority}")]
    [InlineData(nameof(PaymentsController.GetPaymentsByOrder), "orders/{orderId:guid}")]
    [InlineData(nameof(PaymentsController.GetPaymentStatus), "{authority}/status")]
    [InlineData(nameof(PaymentsController.InitiatePayment), null)]
    [InlineData(nameof(PaymentsController.Webhook), "webhooks/{gateway}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(PaymentsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
