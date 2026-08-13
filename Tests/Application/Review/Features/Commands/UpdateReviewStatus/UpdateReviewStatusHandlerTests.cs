using Application.Common.Interfaces;
using Application.Review.Features.Commands.UpdateReviewStatus;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.UpdateReviewStatus;

public class UpdateReviewStatusHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IAuditContextEnricher _auditContextEnricher = Substitute.For<IAuditContextEnricher>(); private readonly UpdateReviewStatusHandler _sut;

    public UpdateReviewStatusHandlerTests()
    {
        _sut = new UpdateReviewStatusHandler(_reviewRepository, _auditContextEnricher);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new UpdateReviewStatusCommand(Guid.NewGuid(), "Approved"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenStatusIsUnknown_ReturnsValidationAndDoesNotUpdate()
    {
        var review = new ProductReviewBuilder().Build();
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new UpdateReviewStatusCommand(review.Id.Value, "Bogus"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        review.Status.Value.ShouldBe("Pending");
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("approved")]
    [InlineData("APPROVED")]
    public async Task Handle_WhenStatusIsApproved_ApprovesReviewAndUpdatesRepository(string status)
    {
        var review = new ProductReviewBuilder().Build();
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new UpdateReviewStatusCommand(review.Id.Value, status),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.Status.Value.ShouldBe("Approved");
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("previousStatus", "در انتظار تأیید");
        _auditContextEnricher.Received(1).Set("newStatus", "تأیید شده");
        _auditContextEnricher.Received(1).Set("requestedStatus", status);
    }

    [Fact]
    public async Task Handle_WhenStatusIsRejectedWithReason_RejectsReviewAndPersistsReason()
    {
        var review = new ProductReviewBuilder().Build();
        var reason = "متن نامناسب";

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new UpdateReviewStatusCommand(review.Id.Value, "Rejected", reason),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.Status.Value.ShouldBe("Rejected");
        review.RejectionReason.ShouldBe(reason);
        _reviewRepository.Received(1).Update(review);
        _auditContextEnricher.Received(1).Set("newStatus", "رد شده");
        _auditContextEnricher.Received(1).Set("reason", reason);
    }
}
