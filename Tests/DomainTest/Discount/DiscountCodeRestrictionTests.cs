namespace Tests.DomainTest.Discount;

public class DiscountCodeRestrictionTests
{
    [Fact]
    public void AddCategoryRestriction_ShouldAddRestrictionToCollection()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.AddCategoryRestriction(categoryId: 5);

        discount.Restrictions.Should().HaveCount(1);
        discount.IsRestrictedToCategory(5).Should().BeTrue();
    }

    [Fact]
    public void AddProductRestriction_ShouldAddRestrictionToCollection()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.AddProductRestriction(productId: 10);

        discount.Restrictions.Should().HaveCount(1);
        discount.IsRestrictedToProduct(10).Should().BeTrue();
    }

    [Fact]
    public void AddUserRestriction_ShouldAddRestrictionToCollection()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.AddUserRestriction(userId: 1);

        discount.Restrictions.Should().HaveCount(1);
        discount.IsRestrictedToUser(1).Should().BeTrue();
    }

    [Fact]
    public void AddBrandRestriction_ShouldAddRestrictionToCollection()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.AddBrandRestriction(brandId: 3);

        discount.Restrictions.Should().HaveCount(1);
        discount.IsRestrictedToBrand(3).Should().BeTrue();
    }

    [Fact]
    public void AddCategoryRestriction_WhenDuplicate_ShouldNotAddAgain()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.AddCategoryRestriction(categoryId: 5);
        discount.AddCategoryRestriction(categoryId: 5);

        discount.Restrictions.Should().HaveCount(1);
    }

    [Fact]
    public void AddCategoryRestriction_WhenDeleted_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        var act = () => discount.AddCategoryRestriction(categoryId: 5);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void HasRestriction_WhenNoRestrictions_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.HasAnyRestrictions().Should().BeFalse();
    }

    [Fact]
    public void HasAnyRestrictions_WhenRestrictionsExist_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.AddCategoryRestriction(1);

        discount.HasAnyRestrictions().Should().BeTrue();
    }

    [Fact]
    public void RemoveRestriction_WhenExists_ShouldRemoveFromCollection()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.AddCategoryRestriction(categoryId: 5);
        var restrictionId = discount.Restrictions.First().Id;

        discount.RemoveRestriction(restrictionId);

        discount.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRestriction_WhenNotExists_ShouldNotThrow()
    {
        var discount = new DiscountCodeBuilder().Build();

        var act = () => discount.RemoveRestriction(999);

        act.Should().NotThrow();
    }

    [Fact]
    public void ClearRestrictions_ShouldRemoveAllRestrictions()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.AddCategoryRestriction(1);
        discount.AddProductRestriction(10);
        discount.AddUserRestriction(5);

        discount.ClearRestrictions();

        discount.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public void GetRestrictedCategoryIds_ShouldReturnOnlyCategoryIds()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.AddCategoryRestriction(1);
        discount.AddCategoryRestriction(2);
        discount.AddProductRestriction(10);

        var categoryIds = discount.GetRestrictedCategoryIds().ToList();

        categoryIds.Should().HaveCount(2);
        categoryIds.Should().Contain(new[] { 1, 2 });
    }

    [Fact]
    public void GetRestrictedProductIds_ShouldReturnOnlyProductIds()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.AddProductRestriction(10);
        discount.AddProductRestriction(20);
        discount.AddCategoryRestriction(1);

        var productIds = discount.GetRestrictedProductIds().ToList();

        productIds.Should().HaveCount(2);
        productIds.Should().Contain(new[] { 10, 20 });
    }

    [Fact]
    public void AddRestriction_WithInvalidStringType_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();

        var act = () => discount.AddRestriction("InvalidType", entityId: 1);

        act.Should().Throw<DomainException>();
    }
}