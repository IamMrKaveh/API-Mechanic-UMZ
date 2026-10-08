using Application.Review.Features.Commands.CreateReview;
using Mapster;
using Presentation.Review.Mapping;
using Presentation.Review.Requests;

namespace Tests.Presentation.Review.Mapping;

public class ReviewMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly ReviewMappingConfig _sut = new();

    public ReviewMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void CreateReviewRequest_MapsToCommand()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var request = new CreateReviewRequest(productId, orderId, 5, "Great", "Comment");

        // Act
        var command = request.Adapt<CreateReviewCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ProductId.ShouldBe(request.ProductId);
        command.OrderId.ShouldBe(request.OrderId);
        command.Rating.ShouldBe(request.Rating);
        command.Title.ShouldBe(request.Title);
        command.Comment.ShouldBe(request.Comment);
    }

    [Fact]
    public void CreateReviewRequest_WithNulls_MapsToCommand()
    {
        // Arrange
        var request = new CreateReviewRequest(Guid.NewGuid(), null, 4, null, null);

        // Act
        var command = request.Adapt<CreateReviewCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.OrderId.ShouldBeNull();
        command.Title.ShouldBeNull();
        command.Comment.ShouldBeNull();
    }

    [Fact]
    public void ReviewMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
