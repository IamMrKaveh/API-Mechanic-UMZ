using Application.Review.Features.Commands.BulkOperation;
using Application.Review.Features.Commands.RejectReview;
using Mapster;
using Presentation.Review.Mapping;
using Presentation.Review.Requests;

namespace Tests.Presentation.Review.Mapping;

public class AdminReviewMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly AdminReviewMappingConfig _sut = new();

    public AdminReviewMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void RejectReviewRequest_MapsToCommand_IgnoringReviewId()
    {
        // Arrange
        var request = new RejectReviewRequest("spam");

        // Act
        var command = request.Adapt<RejectReviewCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ReviewId.ShouldBe(Guid.Empty);
        command.Reason.ShouldBe(request.Reason);
    }

    [Fact]
    public void BulkReviewActionRequest_MapsToBulkApproveCommand()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new BulkReviewActionRequest(ids);

        // Act
        var command = request.Adapt<BulkApproveReviewsCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ReviewIds.ShouldBe(request.ReviewIds);
    }

    [Fact]
    public void BulkRejectReviewsRequest_MapsToCommand()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        var request = new BulkRejectReviewsRequest(ids, "spam");

        // Act
        var command = request.Adapt<BulkRejectReviewsCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ReviewIds.ShouldBe(request.ReviewIds);
        command.Reason.ShouldBe(request.Reason);
    }

    [Fact]
    public void BulkDeleteReviewsRequest_MapsToCommand()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        var request = new BulkDeleteReviewsRequest(ids, "obsolete");

        // Act
        var command = request.Adapt<BulkDeleteReviewsCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ReviewIds.ShouldBe(request.ReviewIds);
        command.Reason.ShouldBe(request.Reason);
    }

    [Fact]
    public void AdminReviewMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
