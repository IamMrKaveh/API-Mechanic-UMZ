using Application.Common.Interfaces;
using Application.Review.Features.Commands.RemoveAdminReply;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.RemoveAdminReply;

public class RemoveAdminReplyHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly RemoveAdminReplyHandler _sut;

    public RemoveAdminReplyHandlerTests()
    {
        _sut = new RemoveAdminReplyHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new RemoveAdminReplyCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenReviewHasExistingReply_RemovesReplyAndEnrichesAudit()
    {
        var review = new ProductReviewBuilder().Build();
        var existingReply = "پاسخ برای حذف";
        review.AddAdminReply(existingReply);

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new RemoveAdminReplyCommand(review.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.AdminReply.ShouldBeNull();
        review.RepliedAt.ShouldBeNull();
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previouslyHadReply", "True");
        _auditContextEnricher.Received(1).Set("previousReplyLength", existingReply.Length.ToString());
        _auditContextEnricher.Received(1).Set("reviewId", review.Id.Value.ToString());
        _auditContextEnricher.Received(1).Set("productId", review.ProductId.Value.ToString());
    }

    [Fact]
    public async Task Handle_WhenReviewHasNoExistingReply_ReturnsSuccessAsNoOp()
    {
        var review = new ProductReviewBuilder().Build();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new RemoveAdminReplyCommand(review.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.AdminReply.ShouldBeNull();
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previouslyHadReply", "False");
        _auditContextEnricher.Received(1).Set("previousReplyLength", "0");
    }
}
