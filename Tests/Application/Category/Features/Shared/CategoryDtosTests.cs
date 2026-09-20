using Application.Category.Features.Shared;

namespace Tests.Application.Category.Features.Shared;

public class CategoryDtosTests
{
    [Fact]
    public void CategoryDto_Defaults_AreEmpty()
    {
        var dto = new CategoryDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Name.ShouldBeNull();
        dto.Slug.ShouldBeNull();
        dto.Description.ShouldBeNull();
        dto.IconUrl.ShouldBeNull();
        dto.IsActive.ShouldBeFalse();
        dto.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void CategoryDetailDto_DefaultChildren_IsEmpty()
    {
        var dto = new CategoryDetailDto();

        dto.Name.ShouldBe(string.Empty);
        dto.Children.ShouldNotBeNull();
        dto.Children.ShouldBeEmpty();
        dto.ProductCount.ShouldBe(0);
        dto.BrandCount.ShouldBe(0);
        dto.RowVersion.ShouldBeNull();
    }

    [Fact]
    public void CategoryDetailDto_WithChildren_PreservesHierarchy()
    {
        var dto = new CategoryDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Engine",
            Children = new List<CategoryTreeDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Pistons", Slug = "pistons", IsActive = true, SortOrder = 1 }
            }
        };

        dto.Children.Count.ShouldBe(1);
        dto.Children[0].Name.ShouldBe("Pistons");
    }

    [Fact]
    public void CategoryListItemDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var dto = new CategoryListItemDto
        {
            Id = id,
            Name = "Brakes",
            Slug = "brakes",
            IconUrl = "/icons/brakes.png",
            IsActive = true,
            IsDeleted = false,
            SortOrder = 2,
            ProductCount = 15,
            CreatedAt = new DateTime(2026, 1, 1),
            UpdatedAt = new DateTime(2026, 1, 2),
            RowVersion = "AAA"
        };

        dto.Id.ShouldBe(id);
        dto.Name.ShouldBe("Brakes");
        dto.ProductCount.ShouldBe(15);
        dto.RowVersion.ShouldBe("AAA");
    }

    [Fact]
    public void CategoryTreeDto_NestedChildren_RoundTrip()
    {
        var dto = new CategoryTreeDto
        {
            Id = Guid.NewGuid(),
            Name = "Root",
            Slug = "root",
            IsActive = true,
            SortOrder = 0,
            Children = new List<CategoryTreeDto>
            {
                new()
                {
                    Id = Guid.NewGuid(), Name = "Child", Slug = "child", IsActive = true, SortOrder = 1,
                    Children = new List<CategoryTreeDto>
                    {
                        new() { Id = Guid.NewGuid(), Name = "GrandChild", Slug = "grandchild", IsActive = true, SortOrder = 0 }
                    }
                }
            }
        };

        dto.Children.Count.ShouldBe(1);
        dto.Children[0].Children.Count.ShouldBe(1);
        dto.Children[0].Children[0].Name.ShouldBe("GrandChild");
    }

    [Fact]
    public void CategoryWithBrandsDto_DefaultBrands_IsEmpty()
    {
        var dto = new CategoryWithBrandsDto();

        dto.Brands.ShouldNotBeNull();
        dto.Brands.ShouldBeEmpty();
        dto.Name.ShouldBe(string.Empty);
    }

    [Fact]
    public void BrandInCategoryDto_InitProperties_RoundTrip()
    {
        var dto = new BrandInCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = "Bosch",
            Slug = "bosch",
            LogoPath = "/l.png",
            IsActive = true
        };

        dto.Name.ShouldBe("Bosch");
        dto.Slug.ShouldBe("bosch");
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void CategoryProductItemDto_InitProperties_RoundTrip()
    {
        var dto = new CategoryProductItemDto
        {
            Id = Guid.NewGuid(),
            Name = "Brake Pad",
            BrandName = "Bosch",
            MaxPrice = 500m,
            MinPrice = 100m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 5)
        };

        dto.BrandName.ShouldBe("Bosch");
        dto.MaxPrice.ShouldBe(500m);
        dto.MinPrice.ShouldBe(100m);
    }
}
