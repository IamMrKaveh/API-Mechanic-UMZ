using Application.Brand.Features.Shared;

namespace Tests.Application.Brand.Features.Shared;

public class BrandDtosTests
{
    [Fact]
    public void BrandDetailDto_Defaults_AreEmpty()
    {
        var dto = new BrandDetailDto();

        dto.Id.ShouldBe(default(Guid));
        dto.CategoryId.ShouldBe(default(Guid));
        dto.Name.ShouldBe(string.Empty);
        dto.Slug.ShouldBeNull();
        dto.Description.ShouldBeNull();
        dto.LogoPath.ShouldBeNull();
        dto.CategoryName.ShouldBe(string.Empty);
        dto.IsActive.ShouldBeFalse();
        dto.ProductCount.ShouldBe(0);
        dto.ActiveProductCount.ShouldBe(0);
        dto.CreatedAt.ShouldBe(default);
        dto.UpdatedAt.ShouldBeNull();
        dto.RowVersion.ShouldBeNull();
    }

    [Fact]
    public void BrandDetailDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var dto = new BrandDetailDto
        {
            Id = id,
            CategoryId = categoryId,
            Name = "Bosch",
            Slug = "bosch",
            Description = "Tools",
            LogoPath = "/logos/bosch.png",
            CategoryName = "Tools",
            IsActive = true,
            ProductCount = 42,
            ActiveProductCount = 40,
            CreatedAt = createdAt,
            UpdatedAt = createdAt.AddHours(1),
            RowVersion = "AAAA"
        };

        dto.Id.ShouldBe(id);
        dto.Name.ShouldBe("Bosch");
        dto.Slug.ShouldBe("bosch");
        dto.ProductCount.ShouldBe(42);
        dto.RowVersion.ShouldBe("AAAA");
    }

    [Fact]
    public void BrandListItemDto_Defaults_AreEmpty()
    {
        var dto = new BrandListItemDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Name.ShouldBe(string.Empty);
        dto.Slug.ShouldBeNull();
        dto.CategoryId.ShouldBe(default(Guid));
        dto.CategoryName.ShouldBe(string.Empty);
        dto.IsActive.ShouldBeFalse();
        dto.ProductCount.ShouldBe(0);
        dto.LogoPath.ShouldBeNull();
    }

    [Fact]
    public void BrandListItemDto_NullableSlug_AcceptsValueAndNull()
    {
        var dto = new BrandListItemDto { Id = Guid.NewGuid(), Name = "X", Slug = "x" };
        dto.Slug.ShouldBe("x");

        var without = dto with { Slug = null };
        without.Slug.ShouldBeNull();
        without.Name.ShouldBe("X");
    }

    [Fact]
    public void BrandDetailDto_WithExpression_PreservesOthers()
    {
        var dto = new BrandDetailDto { Id = Guid.NewGuid(), Name = "A", IsActive = true };

        var updated = dto with { IsActive = false };

        updated.IsActive.ShouldBeFalse();
        updated.Name.ShouldBe("A");
    }
}
