using Microsoft.AspNetCore.Http;
using Presentation.Media.Requests;

namespace Tests.Presentation.Media.Requests;

public class MediaRequestsTests
{
    [Fact]
    public void GetAllMediaRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetAllMediaRequest();

        request.EntityType.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
    }

    [Fact]
    public void GetAllMediaRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new GetAllMediaRequest("Product", 2, 25);

        request.EntityType.ShouldBe("Product");
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(25);
    }

    [Fact]
    public void GetAllMediaRequest_IsRecord_EqualityWorks()
    {
        var request1 = new GetAllMediaRequest("Product", 1, 20);
        var request2 = new GetAllMediaRequest("Product", 1, 20);
        var request3 = new GetAllMediaRequest("Category", 1, 20);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void SetPrimaryMediaRequest_WithMediaId_SetsCorrectly()
    {
        var mediaId = Guid.NewGuid();

        var request = new SetPrimaryMediaRequest(mediaId);

        request.MediaId.ShouldBe(mediaId);
    }

    [Fact]
    public void SetPrimaryMediaRequest_IsRecord_EqualityWorks()
    {
        var mediaId = Guid.NewGuid();

        var request1 = new SetPrimaryMediaRequest(mediaId);
        var request2 = new SetPrimaryMediaRequest(mediaId);
        var request3 = new SetPrimaryMediaRequest(Guid.NewGuid());

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void ReorderMediaRequest_WithAllParameters_SetsCorrectly()
    {
        var entityId = Guid.NewGuid();
        var orderedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var request = new ReorderMediaRequest("Product", entityId, orderedIds);

        request.EntityType.ShouldBe("Product");
        request.EntityId.ShouldBe(entityId);
        request.OrderedMediaIds.ShouldBe(orderedIds);
    }

    [Fact]
    public void ReorderMediaRequest_IsRecord_EqualityWorks()
    {
        var entityId = Guid.NewGuid();
        var orderedIds = new List<Guid> { Guid.NewGuid() };

        var request1 = new ReorderMediaRequest("Product", entityId, orderedIds);
        var request2 = new ReorderMediaRequest("Product", entityId, orderedIds);
        var request3 = new ReorderMediaRequest("Product", Guid.NewGuid(), orderedIds);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UploadMediaRequest_WithRequiredValues_SetsCorrectly()
    {
        var entityId = Guid.NewGuid();
        var file = Substitute.For<IFormFile>();
        var request = new UploadMediaRequest
        {
            File = file,
            EntityType = "Product",
            EntityId = entityId,
            IsPrimary = true,
            AltText = "alt"
        };

        request.File.ShouldBe(file);
        request.EntityType.ShouldBe("Product");
        request.EntityId.ShouldBe(entityId);
        request.IsPrimary.ShouldBeTrue();
        request.AltText.ShouldBe("alt");
    }

    [Fact]
    public void UploadMediaRequest_WithDefaults_SetsCorrectly()
    {
        var request = new UploadMediaRequest
        {
            File = Substitute.For<IFormFile>(),
            EntityType = "Product",
            EntityId = Guid.NewGuid()
        };

        request.IsPrimary.ShouldBeFalse();
        request.AltText.ShouldBeNull();
    }
}
