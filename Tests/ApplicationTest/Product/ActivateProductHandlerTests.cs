using Domain.Product.Interfaces;
using ProductBuilder = Tests.Builders.Product.ProductBuilder;

namespace Tests.ApplicationTest.Product;

public class ActivateProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ActivateProductHandler _handler;

    public ActivateProductHandlerTests()
    {
        _currentUserService.UserId.Returns(1);
        _handler = new ActivateProductHandler(_productRepository, _unitOfWork, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Product_Not_Found()
    {
        _productRepository.GetByIdWithVariantsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Product.Product?)null);

        var result = await _handler.Handle(new ActivateProductCommand(999), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Should_Activate_Product_And_Save()
    {
        var product = new ProductBuilder().Build();
        product.Deactivate();

        _productRepository.GetByIdWithVariantsAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _handler.Handle(new ActivateProductCommand(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(product.IsActive);
        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Log_Audit_Event_On_Success()
    {
        var product = new ProductBuilder().Build();
        product.Deactivate();

        _productRepository.GetByIdWithVariantsAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);

        await _handler.Handle(new ActivateProductCommand(product.Id), CancellationToken.None);

        await _auditService.Received(1).LogProductEventAsync(
            product.Id,
            "ActivateProduct",
            Arg.Any<string>(),
            Arg.Any<int?>());
    }

    [Fact]
    public async Task Handle_Should_Return_Success_Even_When_Product_Already_Active()
    {
        var product = new ProductBuilder().Build();

        _productRepository.GetByIdWithVariantsAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _handler.Handle(new ActivateProductCommand(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}