using Domain.Product.Exceptions;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Product.Exceptions;

public class ProductNotAvailableExceptionTests
{
    [Fact]
    public void Construction_WithProductIdAndReasonOnly_ExposesArgumentsAsProperties()
    {
        var id = ProductId.NewId();

        var sut = new ProductNotAvailableException(id, "دلیل");

        sut.ProductId.ShouldBe(id);
        sut.VariantId.ShouldBeNull();
        sut.Message.ShouldContain(id.ToString());
        sut.Message.ShouldContain("دلیل");
    }

    [Fact]
    public void Construction_WithVariantId_ExposesBothIds()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();

        var sut = new ProductNotAvailableException(productId, "دلیل", variantId);

        sut.ProductId.ShouldBe(productId);
        sut.VariantId.ShouldBe(variantId);
        sut.Message.ShouldContain(variantId.ToString());
    }

    [Fact]
    public void ErrorCode_IsProductNotAvailable()
    {
        new ProductNotAvailableException(ProductId.NewId(), "r").ErrorCode
            .ShouldBe("PRODUCT_NOT_AVAILABLE");
    }

    [Fact]
    public void InheritsFromDomainException()
    {
        new ProductNotAvailableException(ProductId.NewId(), "r")
            .ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void Deleted_Factory_ProducesExceptionWithoutVariantId()
    {
        var id = ProductId.NewId();

        var sut = ProductNotAvailableException.Deleted(id);

        sut.ProductId.ShouldBe(id);
        sut.VariantId.ShouldBeNull();
        sut.Message.ShouldContain("حذف");
    }

    [Fact]
    public void Inactive_Factory_ProducesExceptionWithoutVariantId()
    {
        var id = ProductId.NewId();

        var sut = ProductNotAvailableException.Inactive(id);

        sut.ProductId.ShouldBe(id);
        sut.VariantId.ShouldBeNull();
        sut.Message.ShouldContain("غیرفعال");
    }

    [Fact]
    public void VariantInactive_Factory_ProducesExceptionWithVariantId()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();

        var sut = ProductNotAvailableException.VariantInactive(productId, variantId);

        sut.ProductId.ShouldBe(productId);
        sut.VariantId.ShouldBe(variantId);
        sut.Message.ShouldContain("واریانت");
    }

    [Fact]
    public void OutOfStock_Factory_ProducesExceptionWithVariantId()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();

        var sut = ProductNotAvailableException.OutOfStock(productId, variantId);

        sut.ProductId.ShouldBe(productId);
        sut.VariantId.ShouldBe(variantId);
        sut.Message.ShouldContain("موجودی");
    }
}
