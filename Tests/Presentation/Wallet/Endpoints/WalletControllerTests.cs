using Application.Wallet.Features.Commands.ApproveWalletDebit;
using Application.Wallet.Features.Commands.CancelWalletTransfer;
using Application.Wallet.Features.Commands.CancelWithdrawal;
using Application.Wallet.Features.Commands.CompleteWalletTopUp;
using Application.Wallet.Features.Commands.ConfirmWalletTransfer;
using Application.Wallet.Features.Commands.InitiateWalletTopUp;
using Application.Wallet.Features.Commands.InitiateWalletTransfer;
using Application.Wallet.Features.Commands.MarkWithdrawalPaid;
using Application.Wallet.Features.Commands.RejectWalletDebit;
using Application.Wallet.Features.Commands.RejectWithdrawal;
using Application.Wallet.Features.Commands.RequestWithdrawal;
using Application.Wallet.Features.Queries.GetMyWalletDebitRequests;
using Application.Wallet.Features.Queries.GetMyWithdrawals;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Queries.GetWithdrawalById;
using Application.Wallet.Features.Queries.PreviewWalletTransfer;
using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;
using Infrastructure.Common.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Wallet.Endpoints;
using Presentation.Wallet.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Wallet.Endpoints;

public class WalletControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly WalletController _controller;

    public WalletControllerTests()
    {
        var frontendOptions = Options.Create(new FrontendUrlsOptions
        {
            BaseUrl = "https://shop.example",
            LocalHostUrl = "http://localhost:4200",
            WalletTopUpCallbackPath = "/dashboard/wallet/topup/callback"
        });

        _controller = new WalletController(_mediator, Substitute.For<IMapper>(), frontendOptions);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetBalance_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new WalletDto { Id = Guid.NewGuid(), AvailableBalance = 5000 };

        _mediator.Send(Arg.Any<GetWalletBalanceQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WalletDto>.Success(expected));

        // Act
        var result = await _controller.GetBalance(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetLedger_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetWalletLedgerRequest(Page: 1, PageSize: 10);
        var paged = new PaginatedResult<WalletLedgerEntryDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetWalletLedgerQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletLedgerEntryDto>>.Success(paged));

        // Act
        var result = await _controller.GetLedger(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetWalletLedgerQuery>(q => q.UserId == null && q.Page == 1 && !q.IncludeInactiveUsers),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateTopUp_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new InitiateTopUpRequest(100000, "zarinpal");
        var expected = new InitiateTopUpResultDto
        {
            TopUpId = Guid.NewGuid(),
            PaymentUrl = "https://pay.example",
            Authority = "AUTH1",
            Gateway = "zarinpal",
            Amount = 100000
        };

        _mediator.Send(Arg.Any<InitiateWalletTopUpCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InitiateTopUpResultDto>.Success(expected));

        // Act
        var result = await _controller.InitiateTopUp(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<InitiateWalletTopUpCommand>(c => c.Amount == 100000 && c.Gateway == "zarinpal"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TopUpCallback_WithSuccessResult_RedirectsToFrontend()
    {
        // Arrange
        var topUpResult = new CompleteWalletTopUpResult(Guid.NewGuid(), true, "paid", null, 100000, "REF1");

        _mediator.Send(Arg.Any<CompleteWalletTopUpCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CompleteWalletTopUpResult>.Success(topUpResult));

        // Act
        var result = await _controller.TopUpCallback("AUTH1", "OK", CancellationToken.None);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldStartWith("https://shop.example/dashboard/wallet/topup/callback");
        redirect.Url.ShouldContain("status=paid");
        redirect.Url.ShouldContain("authority=AUTH1");
        redirect.Url.ShouldContain("refId=REF1");
        await _mediator.Received(1).Send(
            Arg.Is<CompleteWalletTopUpCommand>(c => c.Authority == "AUTH1" && c.Status == "OK"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TopUpCallback_WithFailureResult_RedirectsWithUnknownStatus()
    {
        // Arrange
        _mediator.Send(Arg.Any<CompleteWalletTopUpCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CompleteWalletTopUpResult>.Failure("failed"));

        // Act
        var result = await _controller.TopUpCallback(null, null, CancellationToken.None);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldContain("status=unknown");
        redirect.Url.ShouldContain("authority=");
    }

    [Fact]
    public async Task CompleteTopUp_WithNulls_UsesDefaults_AndReturnsOk()
    {
        // Arrange
        var request = new CompleteTopUpRequest(null, null);
        var topUpResult = new CompleteWalletTopUpResult(Guid.NewGuid(), true, "paid", null);

        _mediator.Send(Arg.Any<CompleteWalletTopUpCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CompleteWalletTopUpResult>.Success(topUpResult));

        // Act
        var result = await _controller.CompleteTopUp(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CompleteWalletTopUpCommand>(c => c.Authority == string.Empty && c.Status == "NOK"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestWithdrawal_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new RequestWithdrawalRequest(50000, "IR123", "Ali", null);
        var expectedId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RequestWithdrawalCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Guid>.Success(expectedId));

        // Act
        var result = await _controller.RequestWithdrawal(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<Guid>>();
        body.Data.ShouldBe(expectedId);
    }

    [Fact]
    public async Task GetMyWithdrawals_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetWithdrawalsListRequest();
        var paged = new PaginatedResult<WalletWithdrawalRequestDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetMyWithdrawalsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletWithdrawalRequestDto>>.Success(paged));

        // Act
        var result = await _controller.GetMyWithdrawals(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
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
    public async Task CancelWithdrawal_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<CancelWithdrawalCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.CancelWithdrawal(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CancelWithdrawalCommand>(c => c.WithdrawalId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectWithdrawal_WithReason_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new RejectWithdrawalRequest("not owner");

        _mediator.Send(Arg.Any<RejectWithdrawalCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.RejectWithdrawal(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RejectWithdrawalCommand>(c => c.WithdrawalId == id && c.Reason == "not owner"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkWithdrawalPaid_WithReference_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new MarkWithdrawalPaidRequest("REF1");

        _mediator.Send(Arg.Any<MarkWithdrawalPaidCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.MarkWithdrawalPaid(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task PreviewTransfer_WithValidRequest_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var request = new PreviewWalletTransferRequest("09123456789", 50000);
        var expected = new WalletTransferPreviewDto { Amount = 50000 };

        _mediator.Send(Arg.Any<PreviewWalletTransferQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WalletTransferPreviewDto>.Success(expected));

        // Act
        var result = await _controller.PreviewTransfer(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<PreviewWalletTransferQuery>(q => q.RecipientPhoneNumber == "09123456789" && q.Amount == 50000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateTransfer_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new InitiateWalletTransferRequest("09123456789", 50000, "gift");
        var expected = new InitiateWalletTransferResultDto { TransferId = Guid.NewGuid() };

        _mediator.Send(Arg.Any<InitiateWalletTransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InitiateWalletTransferResultDto>.Success(expected));

        // Act
        var result = await _controller.InitiateTransfer(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ConfirmTransfer_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var request = new ConfirmWalletTransferRequest(transferId, "123456");
        var expected = new ConfirmWalletTransferResultDto { TransferId = transferId, Status = "Completed", Amount = 50000 };

        _mediator.Send(Arg.Any<ConfirmWalletTransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ConfirmWalletTransferResultDto>.Success(expected));

        // Act
        var result = await _controller.ConfirmTransfer(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ConfirmWalletTransferCommand>(c => c.TransferId == transferId && c.OtpCode == "123456"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelTransfer_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<CancelWalletTransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.CancelTransfer(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CancelWalletTransferCommand>(c => c.TransferId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyDebitRequests_WithInvalidStatus_SendsNullFilter_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<WalletDebitRequestDto> expected = [];

        _mediator.Send(Arg.Any<GetMyWalletDebitRequestsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<WalletDebitRequestDto>>.Success(expected));

        // Act
        var result = await _controller.GetMyDebitRequests("Nope", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetMyWalletDebitRequestsQuery>(q => q.Status == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyDebitRequests_WithValidStatus_SendsParsedFilter()
    {
        // Arrange
        IReadOnlyList<WalletDebitRequestDto> expected = [];

        _mediator.Send(Arg.Any<GetMyWalletDebitRequestsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<WalletDebitRequestDto>>.Success(expected));

        // Act
        var result = await _controller.GetMyDebitRequests("Pending", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetMyWalletDebitRequestsQuery>(q => q.Status == WalletDebitRequestStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveDebitRequest_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _mediator.Send(Arg.Any<ApproveWalletDebitCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.ApproveDebitRequest(requestId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ApproveWalletDebitCommand>(c => c.RequestId == requestId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectDebitRequest_WithReason_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var body = new RejectWalletDebitRequest("insufficient docs");

        _mediator.Send(Arg.Any<RejectWalletDebitCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.RejectDebitRequest(requestId, body, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RejectWalletDebitCommand>(c => c.RequestId == requestId && c.RejectionReason == "insufficient docs"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WalletController_HasAuthorizeAttribute()
    {
        typeof(WalletController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void WalletController_HasRouteAttribute()
    {
        var routeAttr = typeof(WalletController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/wallet");
    }
}
