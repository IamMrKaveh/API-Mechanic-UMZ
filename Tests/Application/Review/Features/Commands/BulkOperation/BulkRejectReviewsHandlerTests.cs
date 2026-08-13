using Application.Common.Interfaces;
using Application.Review.Features.Commands.BulkOperation;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.BulkOperation;

public class BulkRejectReviewsHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly BulkRejectReviewsHandler _sut;

    public BulkRejectReviewsHandlerTests()
    {
        _sut = new BulkRejectReviewsHandler(_reviewRepository, _unitOfWork);

        _unitOfWork
            .ExecuteStrategyAsync(
                Arg.Any<Func<CancellationToken, Task<ServiceResult<BulkOperationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var op = ci.Arg<Func<CancellationToken, Task<ServiceResult<BulkOperationResult>>>>();
                return op!(ci.Arg<CancellationToken>());
            });
    }

    [Fact]
    public async Task Handle_WhenReviewsFound_RejectsAllWithGivenReasonAndReturnsSuccessCounts()
    {
        var reviewA = new ProductReviewBuilder().Build();
        var reviewB = new ProductReviewBuilder().Build();
        var reason = "توهین‌آمیز";

        _reviewRepository
            .GetByIdAsync(
                Arg.Is<ReviewId>(id => id == reviewA.Id),
                Arg.Any<CancellationToken>())
            .Returns(reviewA);
        _reviewRepository
            .GetByIdAsync(
                Arg.Is<ReviewId>(id => id == reviewB.Id),
                Arg.Any<CancellationToken>())
            .Returns(reviewB);

        var result = await _sut.Handle(
            new BulkRejectReviewsCommand(new[] { reviewA.Id.Value, reviewB.Id.Value }, reason),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.SuccessCount.ShouldBe(2);
        result.Value.FailedCount.ShouldBe(0);
        reviewA.Status.Value.ShouldBe("Rejected");
        reviewB.Status.Value.ShouldBe("Rejected");
        reviewA.RejectionReason.ShouldBe(reason);
        reviewB.RejectionReason.ShouldBe(reason);
    }

    [Fact]
    public async Task Handle_WhenSomeIdsMissing_ReportsMissingIdsAsFailures()
    {
        var existing = new ProductReviewBuilder().Build();
        var missingId = Guid.NewGuid();

        _reviewRepository
            .GetByIdAsync(
                Arg.Is<ReviewId>(id => id == existing.Id),
                Arg.Any<CancellationToken>())
            .Returns(existing);
        _reviewRepository
            .GetByIdAsync(
                Arg.Is<ReviewId>(id => id! == missingId),
                Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new BulkRejectReviewsCommand(new[] { existing.Id.Value, missingId }, "reason"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.SuccessCount.ShouldBe(1);
        result.Value.FailedCount.ShouldBe(1);
        result.Value.FailedIds.ShouldContain(missingId);
    }
}
