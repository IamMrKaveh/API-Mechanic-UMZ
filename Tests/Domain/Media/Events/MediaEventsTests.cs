using Domain.Media.Events;
using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Media.Events;

public class MediaEventsTests
{
    [Fact]
    public void MediaCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = MediaId.NewId();
        var entityId = Guid.NewGuid();

        var sut = new MediaCreatedEvent(id, "Product", entityId);

        sut.MediaId.ShouldBe(id);
        sut.EntityType.ShouldBe("Product");
        sut.EntityId.ShouldBe(entityId);
    }

    [Fact]
    public void MediaDeletedEvent_WithDeleter_StoresDeleter()
    {
        var deleter = UserId.NewId();
        var entityId = Guid.NewGuid();

        var sut = new MediaDeletedEvent(MediaId.NewId(), "Product", entityId, deleter);

        sut.EntityType.ShouldBe("Product");
        sut.EntityId.ShouldBe(entityId);
        sut.DeletedBy.ShouldBe(deleter);
    }

    [Fact]
    public void MediaDeletedEvent_WithoutDeleter_StoresNull()
    {
        var sut = new MediaDeletedEvent(MediaId.NewId(), "Review", Guid.NewGuid(), null);

        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void MediaSetAsPrimaryEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = MediaId.NewId();
        var entityId = Guid.NewGuid();

        var sut = new MediaSetAsPrimaryEvent(id, "Product", entityId);

        sut.MediaId.ShouldBe(id);
        sut.EntityType.ShouldBe("Product");
        sut.EntityId.ShouldBe(entityId);
    }

    [Fact]
    public void MediaEvents_HaveUniqueEventIds()
    {
        var first = new MediaCreatedEvent(MediaId.NewId(), "Product", Guid.NewGuid());
        var second = new MediaCreatedEvent(MediaId.NewId(), "Product", Guid.NewGuid());

        first.EventId.ShouldNotBe(second.EventId);
    }
}
