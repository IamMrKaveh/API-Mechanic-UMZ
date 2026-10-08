using Application.Review.Features.Commands.CreateReview;
using Application.Review.Features.Commands.DeleteOwnReview;
using Application.Review.Features.Commands.DislikeReview;
using Application.Review.Features.Commands.LikeReview;
using Application.Review.Features.Commands.RemoveReviewVote;
using Application.Review.Features.Commands.UpdateOwnReview;
using Application.Review.Features.Queries.CanReviewProduct;
using Application.Review.Features.Queries.GetProductReviews;
using Application.Review.Features.Queries.GetProductReviewSummary;
using Application.Review.Features.Queries.GetReviewById;
using Application.Review.Features.Queries.GetUserReviews;
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

public class ReviewsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ReviewsController _controller;

    public ReviewsControllerTests()
    {
        _controller = new ReviewsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetReviews_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var paged = new PaginatedResult<ProductReviewDto>
        {
            Items = [new ProductReviewDto { Id = Guid.NewGuid(), ProductId = productId, Rating = 5 }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetProductReviewsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductReviewDto>>.Success(paged));

        // Act
        var result = await _controller.GetReviews(productId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetProductReviewsQuery>(q => q.ProductId == productId && q.SortBy == "Newest"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSummary_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expected = new ReviewSummaryDto { ProductId = productId, AverageRating = 4.5, TotalReviews = 10 };

        _mediator.Send(Arg.Is<GetProductReviewSummaryQuery>(q => q.ProductId == productId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ReviewSummaryDto>.Success(expected));

        // Act
        var result = await _controller.GetSummary(productId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<ReviewSummaryDto>>();
        body.Data!.AverageRating.ShouldBe(4.5);
    }

    [Fact]
    public async Task CanReview_WithOrderId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var expected = new CanReviewDto(true, false, true, null);

        _mediator.Send(Arg.Any<CanReviewProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CanReviewDto>.Success(expected));

        // Act
        var result = await _controller.CanReview(productId, orderId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CanReviewProductQuery>(q => q.ProductId == productId && q.OrderId == orderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReview_MapsRequestToCommand_AndReturnsCreated()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateReviewRequest(productId, null, 5, "Great", "Comment");
        var command = new CreateReviewCommand(productId, null, 5, "Great", "Comment");
        var expected = new ProductReviewDto { Id = Guid.NewGuid(), ProductId = productId, Rating = 5 };

        _mapper.Map<CreateReviewCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<CreateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ProductReviewDto>.Success(expected));

        // Act
        var result = await _controller.CreateReview(request, CancellationToken.None);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<ProductReviewDto>>();
        body.Data!.Rating.ShouldBe(5);
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
    public async Task UpdateOwn_MapsAndEnrichesWithRouteId_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var request = new UpdateOwnReviewRequest(4, "Title", "Comment");
        var mapped = new UpdateOwnReviewCommand(Guid.Empty, 4, "Title", "Comment");

        _mapper.Map<UpdateOwnReviewCommand>(request).Returns(mapped);
        _mediator.Send(Arg.Any<UpdateOwnReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateOwn(reviewId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateOwnReviewCommand>(c => c.ReviewId == reviewId && c.Rating == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteOwn_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteOwnReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteOwn(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteOwnReviewCommand>(c => c.ReviewId == reviewId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyReviews_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<ProductReviewDto>
        {
            Items = [new ProductReviewDto { Id = Guid.NewGuid(), Rating = 5 }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetUserReviewsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductReviewDto>>.Success(paged));

        // Act
        var result = await _controller.GetMyReviews();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetUserReviewsQuery>(q => q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LikeReview_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<LikeReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.LikeReview(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<LikeReviewCommand>(c => c.ReviewId == reviewId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DislikeReview_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<DislikeReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DislikeReview(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DislikeReviewCommand>(c => c.ReviewId == reviewId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveReviewVote_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RemoveReviewVoteCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RemoveReviewVote(reviewId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RemoveReviewVoteCommand>(c => c.ReviewId == reviewId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ReviewsController_HasRouteAttribute()
    {
        var routeAttr = typeof(ReviewsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/reviews");
    }

    [Theory]
    [InlineData(nameof(ReviewsController.GetReviews), "products/{productId:guid}")]
    [InlineData(nameof(ReviewsController.GetSummary), "products/{productId:guid}/summary")]
    [InlineData(nameof(ReviewsController.CanReview), "products/{productId:guid}/can-review")]
    [InlineData(nameof(ReviewsController.CreateReview), null)]
    [InlineData(nameof(ReviewsController.GetById), "{reviewId:guid}")]
    [InlineData(nameof(ReviewsController.UpdateOwn), "{reviewId:guid}")]
    [InlineData(nameof(ReviewsController.DeleteOwn), "me/{reviewId:guid}")]
    [InlineData(nameof(ReviewsController.GetMyReviews), "me")]
    [InlineData(nameof(ReviewsController.LikeReview), "{reviewId:guid}/like")]
    [InlineData(nameof(ReviewsController.DislikeReview), "{reviewId:guid}/dislike")]
    [InlineData(nameof(ReviewsController.RemoveReviewVote), "{reviewId:guid}/vote")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(ReviewsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }

    [Theory]
    [InlineData(nameof(ReviewsController.GetReviews))]
    [InlineData(nameof(ReviewsController.CreateReview))]
    [InlineData(nameof(ReviewsController.LikeReview))]
    public void Actions_HaveReviewRateLimitAttribute(string methodName)
    {
        var method = typeof(ReviewsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(ReviewRateLimitAttribute), false).Length.ShouldBeGreaterThan(0);
    }
}
