using Application.Common.Interfaces;
using Application.Review.Features.Commands.ReplyToReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.ReplyToReview;

public class ReplyToReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly ReplyToReviewHandler _sut;

    public ReplyToReviewHandlerTests()
    {
        _sut = new ReplyToReviewHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new ReplyToReviewCommand(Guid.NewGuid(), "reply"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenReviewExists_AddsAdminReplyAutoApprovesAndEnrichesAudit()
    {
        var review = new ProductReviewBuilder().Build();
        var reply = "با تشکر از بازخورد شما";

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new ReplyToReviewCommand(review.Id.Value, reply),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.AdminReply.ShouldBe(reply);
        review.Status.Value.ShouldBe("Approved");
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previousStatus", "در انتظار تأیید");
        _auditContextEnricher.Received(1).Set("newStatus", "تأیید شده");
        _auditContextEnricher.Received(1).Set("previouslyHadReply", "False");
        _auditContextEnricher.Received(1).Set("reviewId", review.Id.Value.ToString());
        _auditContextEnricher.Received(1).Set("productId", review.ProductId.Value.ToString());
        _auditContextEnricher.Received(1).Set("replyLength", reply.Length.ToString());
    }
}
