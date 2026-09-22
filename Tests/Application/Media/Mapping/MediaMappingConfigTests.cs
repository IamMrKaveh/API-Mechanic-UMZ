using Application.Media.Features.Shared;
using Application.Media.Mapping;
using Mapster;

namespace Tests.Application.Media.Mapping;

public class MediaMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public MediaMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new MediaMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Media_ToMediaDto_MapsAllFields()
    {
        var entityId = Guid.NewGuid();
        var media = new MediaBuilder()
            .WithFilePath("uploads/abc/img.png")
            .WithFileName("img.png")
            .WithFileType("image/png")
            .WithFileSize(2048)
            .WithEntityType("Product")
            .WithEntityId(entityId)
            .WithSortOrder(3)
            .WithAltText("alt text")
            .Build();

        var dto = _mapper.Map<MediaDto>(media);

        dto.Id.ShouldBe(media.Id.Value);
        dto.FilePath.ShouldBe(media.FilePath);
        dto.FileName.ShouldBe("img.png");
        dto.FileType.ShouldBe("image/png");
        dto.FileSize.ShouldBe(2048);
        dto.EntityType.ShouldBe("Product");
        dto.EntityId.ShouldBe(entityId);
        dto.SortOrder.ShouldBe(3);
        dto.AltText.ShouldBe("alt text");
        dto.IsActive.ShouldBeTrue();
        dto.CreatedAt.ShouldBe(media.CreatedAt);
    }

    [Fact]
    public void Map_PrimaryMedia_MapsIsPrimaryTrue()
    {
        var media = new MediaBuilder().BuildPrimary();

        var dto = _mapper.Map<MediaDto>(media);

        dto.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new MediaMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
