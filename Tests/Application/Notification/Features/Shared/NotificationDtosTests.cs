using Application.Notification.Features.Shared;

namespace Tests.Application.Notification.Features.Shared;

public class NotificationDtosTests
{
    [Fact]
    public void NotificationDto_Defaults_AreEmpty()
    {
        var dto = new NotificationDto();

        dto.Id.ShouldBe(default(Guid));
        dto.UserId.ShouldBe(default(Guid));
        dto.Title.ShouldBe(string.Empty);
        dto.Message.ShouldBe(string.Empty);
        dto.Type.ShouldBe(string.Empty);
        dto.ActionUrl.ShouldBeNull();
        dto.IsRead.ShouldBeFalse();
        dto.CreatedAt.ShouldBe(default);
    }

    [Fact]
    public void NotificationDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new NotificationDto
        {
            Id = id, UserId = userId, Title = "Order shipped",
            Message = "Your order shipped", Type = "OrderShipped",
            ActionUrl = "/orders/1", IsRead = true, CreatedAt = new DateTime(2026, 4, 1)
        };

        dto.Id.ShouldBe(id);
        dto.UserId.ShouldBe(userId);
        dto.IsRead.ShouldBeTrue();
        dto.ActionUrl.ShouldBe("/orders/1");
    }

    [Fact]
    public void NotificationDto_WithExpression_PreservesOthers()
    {
        var dto = new NotificationDto { Id = Guid.NewGuid(), Title = "T", IsRead = false };

        var updated = dto with { IsRead = true };

        updated.IsRead.ShouldBeTrue();
        updated.Title.ShouldBe("T");
    }
}
