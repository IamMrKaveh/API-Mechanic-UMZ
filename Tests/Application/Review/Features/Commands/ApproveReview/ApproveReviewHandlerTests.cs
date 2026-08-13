using Application.Common.Interfaces;
using Application.Review.Features.Commands.ApproveReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.ApproveReview;

public class ApproveReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly ApproveReviewHandler _sut;

    public ApproveReviewHandlerTests()
    {
        _sut = new ApproveReviewHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(new ApproveReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenReviewExists_ApprovesReviewUpdatesRepositoryAndEnrichesAudit()
    {
        var review = new ProductReviewBuilder().Build();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(new ApproveReviewCommand(review.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        review.Status.Value.ShouldBe("Approved");
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previousStatus", "در انتظار تأیید");
        _auditContextEnricher.Received(1).Set("newStatus", "تأیید شده");
        _auditContextEnricher.Received(1).Set("reviewId", review.Id.Value.ToString());
        _auditContextEnricher.Received(1).Set("productId", review.ProductId.Value.ToString());
    }

    [Fact]
    public async Task Handle_PassesReviewIdBuiltFromRequestIdToRepositoryLookup()
    {
        var id = Guid.NewGuid();
        ReviewId? captured = null;

        _reviewRepository
            .GetByIdAsync(
                Arg.Do<ReviewId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        _ = await _sut.Handle(new ApproveReviewCommand(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
