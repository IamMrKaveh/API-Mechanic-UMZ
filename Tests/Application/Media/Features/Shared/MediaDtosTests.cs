using Application.Media.Features.Shared;

namespace Tests.Application.Media.Features.Shared;

public class MediaDtosTests
{
    [Fact]
    public void MediaDto_Defaults_AreEmpty()
    {
        var dto = new MediaDto();

        dto.Id.ShouldBe(default(Guid));
        dto.FilePath.ShouldBe(string.Empty);
        dto.FileName.ShouldBe(string.Empty);
        dto.FileType.ShouldBe(string.Empty);
        dto.FileSize.ShouldBe(0);
        dto.EntityType.ShouldBe(string.Empty);
        dto.EntityId.ShouldBe(default(Guid));
        dto.AltText.ShouldBeNull();
        dto.PublicUrl.ShouldBeNull();
        dto.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void MediaDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var dto = new MediaDto
        {
            Id = id, FilePath = "uploads/a/img.png", FileName = "img.png",
            FileType = "image/png", FileSize = 1024, EntityType = "Product",
            EntityId = entityId, SortOrder = 2, IsPrimary = true,
            AltText = "alt", IsActive = true, PublicUrl = "https://cdn/x.png",
            CreatedAt = new DateTime(2026, 1, 1)
        };

        dto.Id.ShouldBe(id);
        dto.EntityId.ShouldBe(entityId);
        dto.IsPrimary.ShouldBeTrue();
        dto.PublicUrl.ShouldBe("https://cdn/x.png");
    }

    [Fact]
    public void MediaDto_WithExpression_PreservesOthers()
    {
        var dto = new MediaDto { Id = Guid.NewGuid(), FileName = "a.png", IsPrimary = false };

        var updated = dto with { IsPrimary = true };

        updated.IsPrimary.ShouldBeTrue();
        updated.FileName.ShouldBe("a.png");
    }
}
