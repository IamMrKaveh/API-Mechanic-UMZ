using Application.Common.Interfaces;
using Application.Review.Features.Commands.UpdateAdminReply;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.UpdateAdminReply;

public class UpdateAdminReplyHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly UpdateAdminReplyHandler _sut;

    public UpdateAdminReplyHandlerTests()
    {
        _sut = new UpdateAdminReplyHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new UpdateAdminReplyCommand(Guid.NewGuid(), "new reply"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenReviewHasExistingReply_UpdatesReplyAndEnrichesAudit()
    {
        var review = new ProductReviewBuilder().Build();
        var initialReply = "پاسخ اولیه";
        review.AddAdminReply(initialReply);

        var newReply = "پاسخ به‌روز شده";

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new UpdateAdminReplyCommand(review.Id.Value, newReply),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.AdminReply.ShouldBe(newReply);
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previousReplyLength", initialReply.Length.ToString());
        _auditContextEnricher.Received(1).Set("newReplyLength", newReply.Length.ToString());
        _auditContextEnricher.Received(1).Set("reviewId", review.Id.Value.ToString());
        _auditContextEnricher.Received(1).Set("productId", review.ProductId.Value.ToString());
    }
}
