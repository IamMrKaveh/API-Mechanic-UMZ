using Application.Common.Interfaces;
using Application.Review.Features.Commands.BulkOperation;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.BulkOperation;

public class BulkDeleteReviewsHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly BulkDeleteReviewsHandler _sut;

    public BulkDeleteReviewsHandlerTests()
    {
        _sut = new BulkDeleteReviewsHandler(_reviewRepository, _unitOfWork);

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
    public async Task Handle_WhenReviewsFound_MarksAllAsDeletedAndReturnsSuccessCounts()
    {
        var reviewA = new ProductReviewBuilder().BuildApproved();
        var reviewB = new ProductReviewBuilder().BuildApproved();

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
            new BulkDeleteReviewsCommand(new[] { reviewA.Id.Value, reviewB.Id.Value }),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.SuccessCount.ShouldBe(2);
        result.Value.FailedCount.ShouldBe(0);
        reviewA.IsDeleted.ShouldBeTrue();
        reviewB.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenSomeIdsMissing_ReportsMissingIdsAsFailures()
    {
        var existing = new ProductReviewBuilder().BuildApproved();
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
            new BulkDeleteReviewsCommand(new[] { existing.Id.Value, missingId }),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.SuccessCount.ShouldBe(1);
        result.Value.FailedCount.ShouldBe(1);
        result.Value.FailedIds.ShouldContain(missingId);
        existing.IsDeleted.ShouldBeTrue();
    }
}
