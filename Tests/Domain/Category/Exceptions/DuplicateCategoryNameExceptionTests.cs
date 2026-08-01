using Domain.Category.Exceptions;
using Domain.Category.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Category.Exceptions;

public class DuplicateCategoryNameExceptionTests
{
    [Fact]
    public void Construction_WithCategoryName_ExposesTheName()
    {
        var name = CategoryName.Create("Books");

        var sut = new DuplicateCategoryNameException(name);

        sut.CategoryName.ShouldBe(name);
    }

    [Fact]
    public void ErrorCode_IsDuplicateCategoryName()
    {
        var sut = new DuplicateCategoryNameException(CategoryName.Create("Books"));

        sut.ErrorCode.ShouldBe("DUPLICATE_CATEGORY_NAME");
    }

    [Fact]
    public void Message_ContainsCategoryNameValue()
    {
        var sut = new DuplicateCategoryNameException(CategoryName.Create("Books"));

        sut.Message.ShouldContain("Books");
    }

    [Fact]
    public void InheritsFromDomainException()
    {
        var sut = new DuplicateCategoryNameException(CategoryName.Create("Books"));

        sut.ShouldBeAssignableTo<DomainException>();
    }
}
