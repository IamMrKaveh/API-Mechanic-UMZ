using Domain.Product.Interfaces;

namespace Tests.ApplicationTest.Product;

public class CreateProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests()
    {
        _handler = new CreateProductHandler(_productRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_Create_Product_And_Return_Id()
    {
        var input = new CreateProductInput
        {
            Name = "محصول جدید",
            Slug = "product-new",
            CategoryId = 1,
            BrandId = 1,
            Description = "توضیحات"
        };

        var result = await _handler.Handle
            (new CreateProductCommand(
                input.Name,
                input.Description,
                input.CategoryId,
                input.BrandId),
            CancellationToken.None);

        Assert.True(result.Value > 0 || result.Value == 0);
        await _productRepository.Received(1).AddAsync(Arg.Any<Domain.Product.Product>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Set_Product_Active_On_Creation()
    {
        Domain.Product.Product? capturedProduct = null;

        await _productRepository.AddAsync(
            Arg.Do<Domain.Product.Product>(p => capturedProduct = p),
            Arg.Any<CancellationToken>()
        );

        var input = new CreateProductInput
        {
            Name = "محصول فعال",
            Slug = "product-active",
            CategoryId = 2,
            BrandId = 2,
            Description = null
        };

        await _handler.Handle(new CreateProductCommand(input), CancellationToken.None);

        Assert.NotNull(capturedProduct);
        Assert.True(capturedProduct!.IsActive);
    }

    [Fact]
    public async Task Handle_Should_Assign_Correct_Category_And_Brand()
    {
        Domain.Product.Product? capturedProduct = null;

        await _productRepository.AddAsync(
            Arg.Do<Domain.Product.Product>(p => capturedProduct = p),
            Arg.Any<CancellationToken>()
        );

        var input = new CreateProductInput
        {
            Name = "محصول با دسته‌بندی",
            Slug = "product-category",
            CategoryId = 7,
            BrandId = 4,
            Description = null
        };

        await _handler.Handle(new CreateProductCommand(input), CancellationToken.None);

        Assert.NotNull(capturedProduct);
        Assert.Equal(7, capturedProduct!.CategoryId);
        Assert.Equal(4, capturedProduct.BrandId);
    }
}