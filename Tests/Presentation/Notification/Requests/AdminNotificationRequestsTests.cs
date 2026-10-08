using Presentation.Notification.Requests;

namespace Tests.Presentation.Notification.Requests;

public class AdminNotificationRequestsTests
{
    [Fact]
    public void AdminSendNotificationRequest_WithAllParameters_SetsCorrectly()
    {
        var userId = Guid.NewGuid();

        var request = new AdminSendNotificationRequest("T", "M", "Info", "/orders", false, userId);

        request.Title.ShouldBe("T");
        request.Message.ShouldBe("M");
        request.Type.ShouldBe("Info");
        request.ActionUrl.ShouldBe("/orders");
        request.SendToAll.ShouldBe(false);
        request.UserId.ShouldBe(userId);
    }

    [Fact]
    public void AdminSendNotificationRequest_WithSendToAll_SetsCorrectly()
    {
        var request = new AdminSendNotificationRequest("T", "M", "Info", null, true, null);

        request.SendToAll.ShouldBe(true);
        request.UserId.ShouldBeNull();
        request.ActionUrl.ShouldBeNull();
    }

    [Fact]
    public void AdminSendNotificationRequest_IsRecord_EqualityWorks()
    {
        var request1 = new AdminSendNotificationRequest("T", "M", "Info", null, true, null);
        var request2 = new AdminSendNotificationRequest("T", "M", "Info", null, true, null);
        var request3 = new AdminSendNotificationRequest("Other", "M", "Info", null, true, null);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
