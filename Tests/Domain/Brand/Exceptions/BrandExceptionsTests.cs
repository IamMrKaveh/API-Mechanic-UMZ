using Domain.Brand.Exceptions;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Brand.Exceptions;

public class BrandExceptionsTests
{
    [Fact]
    public void BrandNameAlreadyExistsException_ExposesNameAndErrorCode()
    {
        var name = BrandName.Create("Sony");

        var sut = new BrandNameAlreadyExistsException(name);

        sut.Name.ShouldBe(name);
        sut.ErrorCode.ShouldBe("BRAND_NAME_ALREADY_EXISTS");
        sut.Message.ShouldContain("Sony");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void BrandAlreadyActiveException_ExposesBrandIdAndErrorCode()
    {
        var id = BrandId.NewId();

        var sut = new BrandAlreadyActiveException(id);

        sut.BrandId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("BRAND_ALREADY_ACTIVE");
    }

    [Fact]
    public void BrandAlreadyDeactivatedException_ExposesBrandIdAndErrorCode()
    {
        var id = BrandId.NewId();

        var sut = new BrandAlreadyDeactivatedException(id);

        sut.BrandId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("BRAND_ALREADY_DEACTIVATED");
    }

    [Fact]
    public void BrandDomainExceptions_AreDistinctTypes()
    {
        var active = new BrandAlreadyActiveException(BrandId.NewId());
        var deactivated = new BrandAlreadyDeactivatedException(BrandId.NewId());

        active.GetType().ShouldNotBe(deactivated.GetType());
        active.ErrorCode.ShouldNotBe(deactivated.ErrorCode);
    }
}
