using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.DebitWallet;
using Application.Wallet.Features.Commands.DismissFraudAlert;
using Application.Wallet.Features.Commands.ForceFreezeFromFraudAlert;
using Application.Wallet.Features.Commands.FreezeWallet;
using Application.Wallet.Features.Commands.MarkFraudAlertReviewed;
using Application.Wallet.Features.Commands.RequestWalletDebit;
using Application.Wallet.Features.Commands.UnfreezeWallet;
using Application.Wallet.Features.Queries.ExportWalletLedger;
using Application.Wallet.Features.Queries.GetAdminDebitRequests;
using Application.Wallet.Features.Queries.GetFraudAlertById;
using Application.Wallet.Features.Queries.GetFraudAlerts;
using Application.Wallet.Features.Queries.GetOpenFraudAlertsCount;
using Application.Wallet.Features.Queries.GetPendingDebitRequestsByUser;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Queries.GetWalletsOverview;
using Application.Wallet.Features.Queries.GetWalletStatistics;
using Application.Wallet.Features.Queries.GetWalletTransferById;
using Application.Wallet.Features.Queries.GetWalletTransfers;
using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;
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

public class AdminWalletControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminWalletController _controller;

    public AdminWalletControllerTests()
    {
        _controller = new AdminWalletController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetOverview_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetWalletsOverviewRequest(Search: "ali", Page: 1, PageSize: 20);
        var paged = new PaginatedResult<WalletOverviewDto>
        {
            Items = [new WalletOverviewDto(Guid.NewGuid(), Guid.NewGuid(), "Ali", "a@x.com", 1000, 0, 1000, true, null, DateTime.UtcNow, null)],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetWalletsOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletOverviewDto>>.Success(paged));

        // Act
        var result = await _controller.GetOverview(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetWalletsOverviewQuery>(q => q.Search == "ali"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatistics_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new WalletStatisticsDto(100000, 0, 100000, 10, 0, 10, 5000, 2000, 0, 0, 0, DateTime.UtcNow);

        _mediator.Send(Arg.Any<GetWalletStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WalletStatisticsDto>.Success(expected));

        // Act
        var result = await _controller.GetStatistics(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<WalletStatisticsDto>>();
        body.Data!.TotalWalletsCount.ShouldBe(10);
    }

    [Fact]
    public async Task GetBalance_WithUserId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new WalletDto { Id = Guid.NewGuid(), UserId = userId, AvailableBalance = 5000 };

        _mediator.Send(Arg.Is<GetWalletBalanceQuery>(q => q.userId == userId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WalletDto>.Success(expected));

        // Act
        var result = await _controller.GetBalance(userId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetLedger_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new GetAdminWalletLedgerRequest(Page: 1, PageSize: 10);
        var paged = new PaginatedResult<WalletLedgerEntryDto>
        {
            Items = [new WalletLedgerEntryDto(Guid.NewGuid(), Guid.NewGuid(), userId, 1000, 1000, "Credit", "Admin", Guid.NewGuid(), null, DateTime.UtcNow, false)],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetWalletLedgerQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletLedgerEntryDto>>.Success(paged));

        // Act
        var result = await _controller.GetLedger(userId, request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetWalletLedgerQuery>(q => q.UserId == userId && q.IncludeInactiveUsers),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportLedger_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ExportAdminWalletLedgerRequest(Format: "csv", MaxRows: 100);
        var expected = new ExportWalletLedgerResult([], "ledger.csv", "text/csv");

        _mediator.Send(Arg.Any<ExportWalletLedgerQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ExportWalletLedgerResult>.Success(expected));

        // Act
        var result = await _controller.ExportLedger(userId, request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ExportWalletLedgerQuery>(q => q.UserId == userId && q.Format == "csv" && q.MaxRows == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Credit_WithValidRequest_SendsCommandWithGeneratedKeys_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AdminWalletAdjustmentRequest(50000, "bonus", null, AdminWalletAdjustmentType.Compensation, null);

        _mediator.Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.Credit(userId, request, "key-1", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CreditWalletCommand>(c => c.UserId == userId && c.Amount == 50000 && c.IdempotencyKey == "key-1"),
            Arg.Any<CancellationToken>());
        _controller.Response.Headers.ContainsKey("X-Idempotency-Key-Effective").ShouldBeTrue();
    }

    [Fact]
    public async Task DebitImmediate_WithoutConfirmation_ReturnsBadRequest_WithoutMediatorCall()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AdminImmediateDebitRequest(10000, "penalty", null, null, false);

        // Act
        var result = await _controller.DebitImmediate(userId, request, null, "yes", CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<DebitWalletCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DebitImmediate_WithoutHeader_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AdminImmediateDebitRequest(10000, "penalty", null, null, true);

        // Act
        var result = await _controller.DebitImmediate(userId, request, null, null, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<DebitWalletCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DebitImmediate_WithConfirmation_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AdminImmediateDebitRequest(10000, "penalty", null, null, true);

        _mediator.Send(Arg.Any<DebitWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.DebitImmediate(userId, request, "key-2", "yes", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DebitWalletCommand>(c => c.UserId == userId && c.Amount == 10000 && c.IdempotencyKey == "key-2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestDebit_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AdminWalletDebitRequestPayload(20000, "correction", null, 72, null);
        var expectedId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RequestWalletDebitCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Guid>.Success(expectedId));

        // Act
        var result = await _controller.RequestDebit(userId, request, "key-3", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<Guid>>();
        body.Data.ShouldBe(expectedId);
        await _mediator.Received(1).Send(
            Arg.Is<RequestWalletDebitCommand>(c => c.UserId == userId && c.Amount == 20000 && c.IdempotencyKey == "key-3"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPendingDebitRequests_WithUserId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        IReadOnlyList<WalletDebitRequestDto> expected = [];

        _mediator.Send(Arg.Any<GetPendingDebitRequestsByUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<WalletDebitRequestDto>>.Success(expected));

        // Act
        var result = await _controller.GetPendingDebitRequests(userId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetAdminDebitRequests_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetAdminDebitRequestsListRequest(Status: "Pending", Page: 1, PageSize: 20);
        var paged = new PaginatedResult<AdminDebitRequestListItemDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetAdminDebitRequestsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AdminDebitRequestListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetAdminDebitRequests(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetAdminDebitRequestsQuery>(q => q.Status == "Pending"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTransfers_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetAdminTransfersRequest(Status: "Completed", Page: 1, PageSize: 20);
        var paged = new PaginatedResult<WalletTransferDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetWalletTransfersQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletTransferDto>>.Success(paged));

        // Act
        var result = await _controller.GetTransfers(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetWalletTransfersQuery>(q => q.Status == "Completed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTransferById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Is<GetWalletTransferByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WalletTransferDto>.NotFound());

        // Act
        var result = await _controller.GetTransferById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Freeze_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new FreezeWalletRequest("suspicious");

        _mediator.Send(Arg.Any<FreezeWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.Freeze(userId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<FreezeWalletCommand>(c => c.UserId == userId && c.Reason == "suspicious"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unfreeze_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mediator.Send(Arg.Any<UnfreezeWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.Unfreeze(userId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UnfreezeWalletCommand>(c => c.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFraudAlerts_WithValidEnums_SendsParsedQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetFraudAlertsRequest(Status: "Open", Severity: "High", Page: 1, PageSize: 20);
        var paged = new PaginatedResult<WalletFraudAlertDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetFraudAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletFraudAlertDto>>.Success(paged));

        // Act
        var result = await _controller.GetFraudAlerts(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetFraudAlertsQuery>(q => q.Status == FraudAlertStatus.Open && q.Severity == FraudAlertSeverity.High),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFraudAlerts_WithInvalidEnums_SendsNullFilters_AndReturnsOk()
    {
        // Arrange
        var request = new GetFraudAlertsRequest(Status: "Nope", Severity: "Nope");
        var paged = new PaginatedResult<WalletFraudAlertDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetFraudAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WalletFraudAlertDto>>.Success(paged));

        // Act
        var result = await _controller.GetFraudAlerts(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetFraudAlertsQuery>(q => q.Status == null && q.Severity == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOpenFraudAlertsCount_SendsQuery_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<GetOpenFraudAlertsCountQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<int>.Success(2));

        // Act
        var result = await _controller.GetOpenFraudAlertsCount(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<int>>();
        body.Data.ShouldBe(2);
    }

    [Fact]
    public async Task GetFraudAlertById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Is<GetFraudAlertByIdQuery>(q => q.AlertId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WalletFraudAlertDto>.NotFound());

        // Act
        var result = await _controller.GetFraudAlertById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task MarkFraudAlertReviewed_WithNote_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new FraudAlertReviewRequest("checked");

        _mediator.Send(Arg.Any<MarkFraudAlertReviewedCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.MarkFraudAlertReviewed(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<MarkFraudAlertReviewedCommand>(c => c.AlertId == id && c.Note == "checked"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DismissFraudAlert_WithNote_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new FraudAlertDismissRequest("false positive");

        _mediator.Send(Arg.Any<DismissFraudAlertCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.DismissFraudAlert(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DismissFraudAlertCommand>(c => c.AlertId == id && c.Note == "false positive"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForceFreezeFromFraudAlert_WithNote_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ForceFreezeFromFraudAlertRequest("urgent");

        _mediator.Send(Arg.Any<ForceFreezeFromFraudAlertCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        // Act
        var result = await _controller.ForceFreezeFromFraudAlert(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ForceFreezeFromFraudAlertCommand>(c => c.AlertId == id && c.AdditionalNote == "urgent"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminWalletController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminWalletController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminWalletController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminWalletController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/wallets");
    }

    [Fact]
    public void AdminWalletController_HasRateLimitingAttribute()
    {
        var attr = typeof(AdminWalletController).GetCustomAttributes(typeof(EnableRateLimitingAttribute), false)
            .OfType<EnableRateLimitingAttribute>()
            .SingleOrDefault();

        attr.ShouldNotBeNull();
        attr!.PolicyName.ShouldBe("admin-wallet");
    }
}
