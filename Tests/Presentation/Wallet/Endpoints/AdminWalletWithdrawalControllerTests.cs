using Application.Wallet.Features.Commands.ApproveWithdrawal;
using Application.Wallet.Features.Commands.MarkWithdrawalPaid;
using Application.Wallet.Features.Commands.RejectWithdrawal;
using Application.Wallet.Features.Queries.GetPendingWithdrawals;
using Application.Wallet.Features.Queries.GetWithdrawalById;
using Application.Wallet.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Wallet.Endpoints;
using Presentation.Wallet.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Wallet.Endpoints;

public class AdminWalletWithdrawalControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminWalletWithdrawalController _controller;

    public AdminWalletWithdrawalControllerTests()
    {
        _controller = new AdminWalletWithdrawalController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetPendingWithdrawals_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<WalletWithdrawalRequestDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetPendingWithdrawalsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletWithdrawalRequestDto>>.Success(paged));

        // Act
        var result = await _controller.GetPendingWithdrawals(new GetPendingWithdrawalsListRequest(), CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetPendingWithdrawalsQuery>(q => q.Page == 1 && q.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWithdrawalById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Is<GetWithdrawalByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WalletWithdrawalRequestDto>.NotFound());

        // Act
        var result = await _controller.GetWithdrawalById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ApproveWithdrawal_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<ApproveWithdrawalCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.ApproveWithdrawal(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ApproveWithdrawalCommand>(c => c.WithdrawalId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectWithdrawal_WithReason_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new RejectWithdrawalRequest("invalid iban");

        _mediator.Send(Arg.Any<RejectWithdrawalCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.RejectWithdrawal(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RejectWithdrawalCommand>(c => c.WithdrawalId == id && c.Reason == "invalid iban"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkWithdrawalPaid_WithReference_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new MarkWithdrawalPaidRequest("REF123");

        _mediator.Send(Arg.Any<MarkWithdrawalPaidCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.MarkWithdrawalPaid(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<MarkWithdrawalPaidCommand>(c => c.WithdrawalId == id && c.BankReferenceNumber == "REF123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminWalletWithdrawalController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminWalletWithdrawalController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminWalletWithdrawalController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminWalletWithdrawalController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/wallets/withdrawals");
    }

    [Fact]
    public void AdminWalletWithdrawalController_HasRateLimitingAttribute()
    {
        var attr = typeof(AdminWalletWithdrawalController).GetCustomAttributes(typeof(EnableRateLimitingAttribute), false)
            .OfType<EnableRateLimitingAttribute>()
            .SingleOrDefault();

        attr.ShouldNotBeNull();
        attr!.PolicyName.ShouldBe("admin-wallet");
    }

    [Theory]
    [InlineData(nameof(AdminWalletWithdrawalController.GetPendingWithdrawals), "pending")]
    [InlineData(nameof(AdminWalletWithdrawalController.GetWithdrawalById), "{id:guid}")]
    [InlineData(nameof(AdminWalletWithdrawalController.ApproveWithdrawal), "{id:guid}/approve")]
    [InlineData(nameof(AdminWalletWithdrawalController.RejectWithdrawal), "{id:guid}/reject")]
    [InlineData(nameof(AdminWalletWithdrawalController.MarkWithdrawalPaid), "{id:guid}/mark-paid")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(AdminWalletWithdrawalController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
