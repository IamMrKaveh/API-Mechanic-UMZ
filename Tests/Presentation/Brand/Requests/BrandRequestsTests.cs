using Presentation.Brand.Requests;

namespace Tests.Presentation.Brand.Requests;

public class BrandRequestsTests
{
    [Fact]
    public void CreateBrandRequest_WithRequiredValues_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var request = new CreateBrandRequest
        {
            CategoryId = categoryId,
            Name = "Brand1",
            Slug = "brand-1",
            Description = "Description"
        };

        request.CategoryId.ShouldBe(categoryId);
        request.Name.ShouldBe("Brand1");
        request.Slug.ShouldBe("brand-1");
        request.Description.ShouldBe("Description");
        request.LogoFile.ShouldBeNull();
    }

    [Fact]
    public void UpdateBrandRequest_WithRequiredValues_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var request = new UpdateBrandRequest
        {
            CategoryId = categoryId,
            Name = "Updated",
            Slug = "updated",
            Description = "Description",
            RowVersion = "rv"
        };

        request.CategoryId.ShouldBe(categoryId);
        request.Name.ShouldBe("Updated");
        request.Slug.ShouldBe("updated");
        request.Description.ShouldBe("Description");
        request.RowVersion.ShouldBe("rv");
        request.LogoFile.ShouldBeNull();
    }

    [Fact]
    public void MoveBrandRequest_WithAllParameters_SetsCorrectly()
    {
        var brandId = Guid.NewGuid();
        var targetCategoryId = Guid.NewGuid();

        var request = new MoveBrandRequest(brandId, targetCategoryId);

        request.BrandId.ShouldBe(brandId);
        request.TargetCategoryId.ShouldBe(targetCategoryId);
    }

    [Fact]
    public void MoveBrandRequest_IsRecord_EqualityWorks()
    {
        var brandId = Guid.NewGuid();
        var targetCategoryId = Guid.NewGuid();

        var request1 = new MoveBrandRequest(brandId, targetCategoryId);
        var request2 = new MoveBrandRequest(brandId, targetCategoryId);
        var request3 = new MoveBrandRequest(Guid.NewGuid(), targetCategoryId);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void GetAdminBrandsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAdminBrandsRequest();

        request.CategoryId.ShouldBeNull();
        request.Search.ShouldBeNull();
        request.IsActive.ShouldBeNull();
        request.IncludeDeleted.ShouldBeFalse();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(10);
    }

    [Fact]
    public void GetAdminBrandsRequest_WithAllParameters_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var request = new GetAdminBrandsRequest
        {
            CategoryId = categoryId,
            Search = "brand",
            IsActive = true,
            IncludeDeleted = true,
            Page = 2,
            PageSize = 25
        };

        request.CategoryId.ShouldBe(categoryId);
        request.Search.ShouldBe("brand");
        request.IsActive.ShouldBe(true);
        request.IncludeDeleted.ShouldBeTrue();
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(25);
    }

    [Fact]
    public void GetPublicBrandsRequest_WithCategoryId_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();

        var request = new GetPublicBrandsRequest(categoryId);

        request.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void GetPublicBrandsRequest_WithNullCategory_SetsCorrectly()
    {
        var request = new GetPublicBrandsRequest(null);

        request.CategoryId.ShouldBeNull();
    }

    [Fact]
    public void GetPublicBrandsRequest_IsRecord_EqualityWorks()
    {
        var categoryId = Guid.NewGuid();

        var request1 = new GetPublicBrandsRequest(categoryId);
        var request2 = new GetPublicBrandsRequest(categoryId);
        var request3 = new GetPublicBrandsRequest(null);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
