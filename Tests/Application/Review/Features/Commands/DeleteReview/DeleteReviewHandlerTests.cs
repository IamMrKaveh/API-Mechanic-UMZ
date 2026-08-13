using Application.Common.Interfaces;
using Application.Review.Features.Commands.DeleteReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.DeleteReview;

public class DeleteReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly DeleteReviewHandler _sut;

    public DeleteReviewHandlerTests()
    {
        _sut = new DeleteReviewHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new DeleteReviewCommand(Guid.NewGuid(), "reason"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenReviewExists_MarksReviewAsDeletedUpdatesRepositoryAndEnrichesAudit()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        var reason = "duplicate content";

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new DeleteReviewCommand(review.Id.Value, reason),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.IsDeleted.ShouldBeTrue();
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previousStatus", "تأیید شده");
        _auditContextEnricher.Received(1).Set("previousIsDeleted", "False");
        _auditContextEnricher.Received(1).Set("newIsDeleted", "True");
        _auditContextEnricher.Received(1).Set("reviewId", review.Id.Value.ToString());
        _auditContextEnricher.Received(1).Set("productId", review.ProductId.Value.ToString());
        _auditContextEnricher.Received(1).Set("ownerUserId", review.UserId.Value.ToString());
        _auditContextEnricher.Received(1).Set("reason", reason);
    }

    [Fact]
    public async Task Handle_WhenReasonIsNullOrWhitespace_DoesNotEnrichReasonInAudit()
    {
        var review = new ProductReviewBuilder().Build();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new DeleteReviewCommand(review.Id.Value, null),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _auditContextEnricher.DidNotReceive().Set("reason", Arg.Any<string?>());
    }
}
