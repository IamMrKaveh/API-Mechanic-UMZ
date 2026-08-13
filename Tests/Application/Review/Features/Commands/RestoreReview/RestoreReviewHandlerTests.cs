using Application.Common.Interfaces;
using Application.Review.Features.Commands.RestoreReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.RestoreReview;

public class RestoreReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly RestoreReviewHandler _sut;

    public RestoreReviewHandlerTests()
    {
        _sut = new RestoreReviewHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        _reviewRepository
            .GetByIdIncludingDeletedAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new RestoreReviewCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_LooksUpReviewUsingGetByIdIncludingDeletedAsync()
    {
        var review = new ProductReviewBuilder().BuildDeleted();

        _reviewRepository
            .GetByIdIncludingDeletedAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        _ = await _sut.Handle(new RestoreReviewCommand(review.Id.Value), CancellationToken.None);

        await _reviewRepository.Received(1)
            .GetByIdIncludingDeletedAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>());
        await _reviewRepository.DidNotReceiveWithAnyArgs()
            .GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenReviewIsDeleted_RestoresReviewAndEnrichesAudit()
    {
        var review = new ProductReviewBuilder().BuildDeleted();
        review.IsDeleted.ShouldBeTrue();

        _reviewRepository
            .GetByIdIncludingDeletedAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(new RestoreReviewCommand(review.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        review.IsDeleted.ShouldBeFalse();
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previousIsDeleted", "True");
        _auditContextEnricher.Received(1).Set("newIsDeleted", "False");
        _auditContextEnricher.Received(1).Set("reviewId", review.Id.Value.ToString());
        _auditContextEnricher.Received(1).Set("productId", review.ProductId.Value.ToString());
        _auditContextEnricher.Received(1).Set("ownerUserId", review.UserId.Value.ToString());
    }
}
