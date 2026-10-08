using Application.Review.Features.Commands.ApproveReview;
using Application.Review.Features.Commands.BulkOperation;
using Application.Review.Features.Commands.DeleteReview;
using Application.Review.Features.Commands.RejectReview;
using Application.Review.Features.Commands.RemoveAdminReply;
using Application.Review.Features.Commands.ReplyToReview;
using Application.Review.Features.Commands.RestoreReview;
using Application.Review.Features.Commands.UpdateAdminReply;
using Application.Review.Features.Commands.UpdateReviewStatus;
using Application.Review.Features.Queries.AdminReviewStats;
using Application.Review.Features.Queries.GetReviewById;
using Application.Review.Features.Queries.GetReviewsByStatus;
using Application.Review.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Filters;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Review.Endpoints;
using Presentation.Review.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Review.Endpoints;

public class AdminReviewsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminReviewsController _controller;

    public AdminReviewsControllerTests()
    {
        _controller = new AdminReviewsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetReviewsByStatus_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<ProductReviewDto>
        {
            Items = [new ProductReviewDto { Id = Guid.NewGuid(), Rating = 5 }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetReviewsByStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductReviewDto>>.Success(paged));

        // Act
        var result = await _controller.GetReviewsByStatus();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetReviewsByStatusQuery>(q => q.Status == "Pending" && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStats_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new AdminReviewStatsDto(1, 2, 3, 6);

        _mediator.Send(Arg.Any<GetAdminReviewStatsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminReviewStatsDto>.Success(expected));

        // Act
        var result = await _controller.GetStats(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<GetAdminReviewStatsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var expected = new ProductReviewDto { Id = reviewId, Rating = 4 };

        _mediator.Send(Arg.Is<GetReviewByIdQuery>(q => q.ReviewId == reviewId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ProductReviewDto>.Success(expected));

        // Act
        var result = await _controller.GetById(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ApproveReview_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<ApproveReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ApproveReview(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ApproveReviewCommand>(c => c.ReviewId == reviewId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectReview_MapsAndEnrichesWithRouteId_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var request = new RejectReviewRequest("spam");
        var mapped = new RejectReviewCommand(Guid.Empty, "spam");

        _mapper.Map<RejectReviewCommand>(request).Returns(mapped);
        _mediator.Send(Arg.Any<RejectReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RejectReview(reviewId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RejectReviewCommand>(c => c.ReviewId == reviewId && c.Reason == "spam"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReviewStatus_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var request = new UpdateReviewStatusRequest("Approved", "ok");

        _mediator.Send(Arg.Any<UpdateReviewStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateReviewStatus(reviewId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateReviewStatusCommand>(c => c.ReviewId == reviewId && c.Status == "Approved"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteReview_WithReason_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteReview(reviewId, "spam", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteReviewCommand>(c => c.ReviewId == reviewId && c.Reason == "spam"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreReview_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RestoreReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RestoreReview(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RestoreReviewCommand>(c => c.ReviewId == reviewId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplyToReview_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var request = new ReplyToReviewRequest("thanks");

        _mediator.Send(Arg.Any<ReplyToReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ReplyToReview(reviewId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ReplyToReviewCommand>(c => c.ReviewId == reviewId && c.Reply == "thanks"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReply_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var request = new ReplyToReviewRequest("updated");

        _mediator.Send(Arg.Any<UpdateAdminReplyCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateReply(reviewId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateAdminReplyCommand>(c => c.ReviewId == reviewId && c.Reply == "updated"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveReply_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RemoveAdminReplyCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RemoveReply(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RemoveAdminReplyCommand>(c => c.ReviewId == reviewId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkApprove_MapsRequestToCommand_AndReturnsOk()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new BulkReviewActionRequest(ids);
        var command = new BulkApproveReviewsCommand(ids);
        var expected = new BulkOperationResult(2, 0, [], []);

        _mapper.Map<BulkApproveReviewsCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<BulkApproveReviewsCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BulkOperationResult>.Success(expected));

        // Act
        var result = await _controller.BulkApprove(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task BulkReject_MapsRequestToCommand_AndReturnsOk()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        var request = new BulkRejectReviewsRequest(ids, "spam");
        var command = new BulkRejectReviewsCommand(ids, "spam");
        var expected = new BulkOperationResult(1, 0, [], []);

        _mapper.Map<BulkRejectReviewsCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<BulkRejectReviewsCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BulkOperationResult>.Success(expected));

        // Act
        var result = await _controller.BulkReject(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task BulkDelete_MapsRequestToCommand_AndReturnsOk()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        var request = new BulkDeleteReviewsRequest(ids, null);
        var command = new BulkDeleteReviewsCommand(ids, null);
        var expected = new BulkOperationResult(1, 0, [], []);

        _mapper.Map<BulkDeleteReviewsCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<BulkDeleteReviewsCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BulkOperationResult>.Success(expected));

        // Act
        var result = await _controller.BulkDelete(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public void AdminReviewsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminReviewsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminReviewsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminReviewsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/reviews");
    }

    [Theory]
    [InlineData(nameof(AdminReviewsController.GetReviewsByStatus), null)]
    [InlineData(nameof(AdminReviewsController.GetStats), "stats")]
    [InlineData(nameof(AdminReviewsController.GetById), "{reviewId:guid}")]
    [InlineData(nameof(AdminReviewsController.ApproveReview), "{reviewId:guid}/approve")]
    [InlineData(nameof(AdminReviewsController.RejectReview), "{reviewId:guid}/reject")]
    [InlineData(nameof(AdminReviewsController.UpdateReviewStatus), "{reviewId:guid}/status")]
    [InlineData(nameof(AdminReviewsController.DeleteReview), "{reviewId:guid}")]
    [InlineData(nameof(AdminReviewsController.RestoreReview), "{reviewId:guid}/restore")]
    [InlineData(nameof(AdminReviewsController.ReplyToReview), "{reviewId:guid}/reply")]
    [InlineData(nameof(AdminReviewsController.UpdateReply), "{reviewId:guid}/reply")]
    [InlineData(nameof(AdminReviewsController.RemoveReply), "{reviewId:guid}/reply")]
    [InlineData(nameof(AdminReviewsController.BulkApprove), "bulk/approve")]
    [InlineData(nameof(AdminReviewsController.BulkReject), "bulk/reject")]
    [InlineData(nameof(AdminReviewsController.BulkDelete), "bulk/delete")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminReviewsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }

    [Theory]
    [InlineData(nameof(AdminReviewsController.GetReviewsByStatus))]
    [InlineData(nameof(AdminReviewsController.ApproveReview))]
    [InlineData(nameof(AdminReviewsController.BulkApprove))]
    public void AdminActions_HaveReviewRateLimitAttribute(string methodName)
    {
        var method = typeof(AdminReviewsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(ReviewRateLimitAttribute), false).Length.ShouldBeGreaterThan(0);
    }
}
