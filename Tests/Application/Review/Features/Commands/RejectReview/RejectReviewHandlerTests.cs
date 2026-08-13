using Application.Common.Interfaces;
using Application.Review.Features.Commands.RejectReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.RejectReview;

public class RejectReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly RejectReviewHandler _sut;

    public RejectReviewHandlerTests()
    {
        _sut = new RejectReviewHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new RejectReviewCommand(Guid.NewGuid(), "spam"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenReviewExists_RejectsReviewUpdatesRepositoryAndEnrichesAudit()
    {
        var review = new ProductReviewBuilder().Build();
        var reason = "این نظر توهین‌آمیز است.";

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new RejectReviewCommand(review.Id.Value, reason),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.Status.Value.ShouldBe("Rejected");
        review.RejectionReason.ShouldBe(reason);
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previousStatus", "در انتظار تأیید");
        _auditContextEnricher.Received(1).Set("newStatus", "رد شده");
        _auditContextEnricher.Received(1).Set("reviewId", review.Id.Value.ToString());
        _auditContextEnricher.Received(1).Set("productId", review.ProductId.Value.ToString());
        _auditContextEnricher.Received(1).Set("reason", reason);
    }

    [Fact]
    public async Task Handle_WhenReasonExceedsTwoHundredChars_TruncatesReasonInAudit()
    {
        var review = new ProductReviewBuilder().Build();
        var longReason = new string('a', 250);

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new RejectReviewCommand(review.Id.Value, longReason),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _auditContextEnricher.Received(1).Set("reason", longReason[..200] + "…");
    }
}
