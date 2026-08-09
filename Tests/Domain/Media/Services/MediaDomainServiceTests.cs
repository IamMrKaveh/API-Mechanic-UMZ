using Domain.Media.Services;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Media.Services;

public class MediaDomainServiceTests
{
    [Theory]
    [InlineData("Product")]
    [InlineData("Brand")]
    [InlineData("Category")]
    public void ValidateFileTypeForEntity_ForImageOnEntityRequiringImages_ReturnsSuccess(string entityType)
    {
        var path = FilePath.Create("uploads/logo.png");

        MediaDomainService.ValidateFileTypeForEntity(entityType, path).ShouldBeSuccess();
    }

    [Theory]
    [InlineData("product")]
    [InlineData("PRODUCT")]
    [InlineData("bRaNd")]
    [InlineData("CATEGORY")]
    public void ValidateFileTypeForEntity_EntityTypeMatchIsCaseInsensitive_ForImageStillReturnsSuccess(string entityType)
    {
        var path = FilePath.Create("uploads/logo.png");

        MediaDomainService.ValidateFileTypeForEntity(entityType, path).ShouldBeSuccess();
    }

    [Theory]
    [InlineData("Product", "uploads/doc.pdf")]
    [InlineData("Brand", "uploads/doc.pdf")]
    [InlineData("Category", "uploads/doc.pdf")]
    [InlineData("Product", "uploads/clip.mp4")]
    [InlineData("Brand", "uploads/clip.mp4")]
    [InlineData("Category", "uploads/clip.mp4")]
    public void ValidateFileTypeForEntity_ForNonImageOnEntityRequiringImages_FailsWithInvalidType(string entityType, string filePath)
    {
        var path = FilePath.Create(filePath);

        MediaDomainService.ValidateFileTypeForEntity(entityType, path)
            .ShouldFailWith("Media.InvalidType");
    }

    [Fact]
    public void ValidateFileTypeForEntity_ForNonImageOnEntityRequiringImages_FailureTypeIsValidation()
    {
        var path = FilePath.Create("uploads/doc.pdf");

        MediaDomainService.ValidateFileTypeForEntity("Product", path)
            .ShouldFailWithType(ErrorType.Validation);
    }

    [Theory]
    [InlineData("uploads/doc.pdf")]
    [InlineData("uploads/clip.mp4")]
    [InlineData("uploads/photo.jpg")]
    public void ValidateFileTypeForEntity_ForUnknownEntityTypeWithSupportedExtension_ReturnsSuccess(string filePath)
    {
        var path = FilePath.Create(filePath);

        MediaDomainService.ValidateFileTypeForEntity("Review", path).ShouldBeSuccess();
    }

    [Fact]
    public void ValidateFileTypeForEntity_ForUnknownEntityTypeWithUnsupportedExtension_FailsWithUnsupportedType()
    {
        var path = FilePath.Create("uploads/archive.xyz");

        MediaDomainService.ValidateFileTypeForEntity("Review", path)
            .ShouldFailWith("Media.UnsupportedType");
    }

    [Fact]
    public void ValidateFileTypeForEntity_ForUnknownEntityTypeWithUnsupportedExtension_FailureTypeIsValidation()
    {
        var path = FilePath.Create("uploads/archive.xyz");

        MediaDomainService.ValidateFileTypeForEntity("Review", path)
            .ShouldFailWithType(ErrorType.Validation);
    }

    [Fact]
    public void SelectNewPrimaryAfterDeletion_WithEmptyCollection_ReturnsNull()
    {
        MediaDomainService.SelectNewPrimaryAfterDeletion([])
            .ShouldBeNull();
    }

    [Fact]
    public void SelectNewPrimaryAfterDeletion_WhenAllRemainingAreDeleted_ReturnsNull()
    {
        var m1 = new MediaBuilder().BuildDeleted();
        var m2 = new MediaBuilder().BuildDeleted();

        MediaDomainService.SelectNewPrimaryAfterDeletion(new[] { m1, m2 }).ShouldBeNull();
    }

    [Fact]
    public void SelectNewPrimaryAfterDeletion_WithSingleActiveCandidate_ReturnsThatCandidate()
    {
        var only = new MediaBuilder().WithSortOrder(2).Build();

        MediaDomainService.SelectNewPrimaryAfterDeletion(new[] { only }).ShouldBe(only);
    }

    [Fact]
    public void SelectNewPrimaryAfterDeletion_PicksActiveWithLowestSortOrder()
    {
        var high = new MediaBuilder().WithSortOrder(10).Build();
        var low = new MediaBuilder().WithSortOrder(0).Build();
        var mid = new MediaBuilder().WithSortOrder(5).Build();

        MediaDomainService.SelectNewPrimaryAfterDeletion(new[] { high, low, mid }).ShouldBe(low);
    }

    [Fact]
    public void SelectNewPrimaryAfterDeletion_ExcludesInactiveEvenIfLowerSortOrder()
    {
        var deletedLow = new MediaBuilder().WithSortOrder(0).BuildDeleted();
        var activeHigher = new MediaBuilder().WithSortOrder(5).Build();

        MediaDomainService.SelectNewPrimaryAfterDeletion(new[] { deletedLow, activeHigher })
            .ShouldBe(activeHigher);
    }

    [Fact]
    public void SelectNewPrimaryAfterDeletion_OnSortOrderTie_PicksEarliestCreatedAt()
    {
        var first = new MediaBuilder().WithSortOrder(1).Build();
        Thread.Sleep(15);
        var second = new MediaBuilder().WithSortOrder(1).Build();

        MediaDomainService.SelectNewPrimaryAfterDeletion(new[] { second, first }).ShouldBe(first);
    }

    [Fact]
    public void MediaEntityTypes_ExposesProductBrandAndCategoryConstants()
    {
        MediaEntityTypes.Product.ShouldBe("Product");
        MediaEntityTypes.Brand.ShouldBe("Brand");
        MediaEntityTypes.Category.ShouldBe("Category");
    }
}
