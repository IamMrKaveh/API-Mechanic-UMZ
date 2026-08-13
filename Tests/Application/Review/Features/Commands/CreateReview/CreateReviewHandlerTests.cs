using Application.Common.Interfaces;
using Application.Review.Configuration;
using Application.Review.Features.Commands.CreateReview;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.Review.Services;
using Microsoft.Extensions.Options;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Products = Domain.Product.Aggregates.Product;

namespace Tests.Application.Review.Features.Commands.CreateReview;

public class CreateReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IPurchaseVerificationService _purchaseVerificationService = Substitute.For<IPurchaseVerificationService>(); private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly ReviewDomainService _reviewDomainService; private readonly IOptions<ReviewSettings> _reviewSettings = Options.Create(new ReviewSettings()); private readonly CreateReviewHandler _sut;

    public CreateReviewHandlerTests()
    {
        _reviewDomainService = new ReviewDomainService(_purchaseVerificationService, _reviewRepository);
        _sut = new CreateReviewHandler(
            _reviewDomainService,
            _reviewRepository,
            _productRepository,
            _currentUser,
            _reviewSettings,
            _mapper);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsUnauthorizedAndDoesNotLoadProduct()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(
            new CreateReviewCommand(Guid.NewGuid(), null, 5, "t", "c"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _productRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
        await _reviewRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFoundAndDoesNotAddReview()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((Products?)null);

        var result = await _sut.Handle(
            new CreateReviewCommand(Guid.NewGuid(), null, 5, "t", "c"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _reviewRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_PassesProductIdBuiltFromRequestProductIdToRepositoryLookup()
    {
        var productId = Guid.NewGuid();
        ProductId? captured = null;

        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _productRepository
            .GetByIdAsync(
                Arg.Do<ProductId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((Products?)null);

        _ = await _sut.Handle(
            new CreateReviewCommand(productId, null, 5, "t", "c"),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(productId);
    }
}
