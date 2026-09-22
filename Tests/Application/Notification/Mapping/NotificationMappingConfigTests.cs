using Application.Notification.Features.Shared;
using Application.Notification.Mapping;
using Mapster;

namespace Tests.Application.Notification.Mapping;

public class NotificationMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public NotificationMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new NotificationMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Notification_ToNotificationDto_MapsAllFields()
    {
        var notification = new NotificationBuilder()
            .WithTitle("Hello")
            .WithMessage("World")
            .WithActionUrl("/orders/9")
            .Build();

        var dto = _mapper.Map<NotificationDto>(notification);

        dto.Id.ShouldBe(notification.Id.Value);
        dto.UserId.ShouldBe(notification.UserId.Value);
        dto.Title.ShouldBe("Hello");
        dto.Message.ShouldBe("World");
        dto.Type.ShouldBe(notification.Type.Value);
        dto.ActionUrl.ShouldBe("/orders/9");
        dto.IsRead.ShouldBeFalse();
        dto.CreatedAt.ShouldBe(notification.CreatedAt);
    }

    [Fact]
    public void Map_ReadNotification_MapsIsReadTrue()
    {
        var notification = new NotificationBuilder().Build();
        notification.MarkAsRead();

        var dto = _mapper.Map<NotificationDto>(notification);

        dto.IsRead.ShouldBeTrue();
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new NotificationMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
