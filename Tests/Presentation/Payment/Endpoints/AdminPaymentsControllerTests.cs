using Application.Payment.Features.Commands.AtomicRefundPayment;
using Application.Payment.Features.Queries.GetAdminPayments;
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
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Payment.Endpoints;

public class AdminPaymentsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminPaymentsController _controller;

    public AdminPaymentsControllerTests()
    {
        _controller = new AdminPaymentsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetPayments_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new AdminPaymentSearchRequest(Status: "Paid");
        var query = new GetAdminPaymentsQuery(null, null, "Paid", null, null, null, 1, 10);
        var paged = new PaginatedResult<PaymentTransactionDto>
        {
            Items = [new PaymentTransactionDto { Id = Guid.NewGuid(), Authority = "A123", Status = "Paid" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mapper.Map<GetAdminPaymentsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetAdminPaymentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<PaymentTransactionDto>>.Success(paged));

        // Act
        var result = await _controller.GetPayments(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<PaymentTransactionDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RefundPayment_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new RefundPaymentRequest("duplicate charge");

        _mediator.Send(Arg.Any<AtomicRefundPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RefundPayment(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AtomicRefundPaymentCommand>(c => c.OrderId == id && c.Reason == "duplicate charge"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefundPayment_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new RefundPaymentRequest("reason");

        _mediator.Send(Arg.Any<AtomicRefundPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound());

        // Act
        var result = await _controller.RefundPayment(id, request, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminPaymentsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminPaymentsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminPaymentsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminPaymentsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/payments");
    }

    [Theory]
    [InlineData(nameof(AdminPaymentsController.GetPayments), null)]
    [InlineData(nameof(AdminPaymentsController.RefundPayment), "{id:guid}/refunds")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminPaymentsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
