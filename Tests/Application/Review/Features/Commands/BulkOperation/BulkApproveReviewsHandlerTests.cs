using Application.Common.Interfaces;
using Application.Review.Features.Commands.BulkOperation;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.BulkOperation;

public class BulkApproveReviewsHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly BulkApproveReviewsHandler _sut;

    public BulkApproveReviewsHandlerTests()
    {
        _sut = new BulkApproveReviewsHandler(_reviewRepository, _unitOfWork);

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
    public async Task Handle_WhenAllReviewsFound_ApprovesAllAndReturnsSuccessCountEqualToInputSize()
    {
        var reviewA = new ProductReviewBuilder().Build();
        var reviewB = new ProductReviewBuilder().Build();

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
            new BulkApproveReviewsCommand(new[] { reviewA.Id.Value, reviewB.Id.Value }),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.SuccessCount.ShouldBe(2);
        result.Value.FailedCount.ShouldBe(0);
        result.Value.FailedIds.ShouldBeEmpty();
        reviewA.Status.Value.ShouldBe("Approved");
        reviewB.Status.Value.ShouldBe("Approved");
        _reviewRepository.Received(1).Update(reviewA);
        _reviewRepository.Received(1).Update(reviewB);
    }

    [Fact]
    public async Task Handle_WhenSomeReviewsNotFound_ReportsMissingIdsAsFailuresAndApprovesTheRest()
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
            new BulkApproveReviewsCommand(new[] { existing.Id.Value, missingId }),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.SuccessCount.ShouldBe(1);
        result.Value.FailedCount.ShouldBe(1);
        result.Value.FailedIds.ShouldContain(missingId);
        result.Value.Failures.ShouldHaveSingleItem();
        result.Value.Failures[0].ReviewId.ShouldBe(missingId);
        result.Value.Failures[0].Error.ShouldBe("نظر یافت نشد.");
    }

    [Fact]
    public async Task Handle_DeduplicatesRepeatedIdsInInput()
    {
        var review = new ProductReviewBuilder().Build();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new BulkApproveReviewsCommand(new[] { review.Id.Value, review.Id.Value, review.Id.Value }),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.SuccessCount.ShouldBe(1);
        result.Value.FailedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ExecutesWorkThroughUnitOfWorkStrategy()
    {
        var review = new ProductReviewBuilder().Build();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        _ = await _sut.Handle(
            new BulkApproveReviewsCommand(new[] { review.Id.Value }),
            CancellationToken.None);

        await _unitOfWork.Received(1).ExecuteStrategyAsync(
            Arg.Any<Func<CancellationToken, Task<ServiceResult<BulkOperationResult>>>>(),
            Arg.Any<CancellationToken>());
    }
}
