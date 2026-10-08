using Presentation.Notification.Requests;

namespace Tests.Presentation.Notification.Requests;

public class NotificationRequestsTests
{
    [Fact]
    public void GetNotificationsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new GetNotificationsRequest();

        request.UnreadOnly.ShouldBeFalse();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
    }

    [Fact]
    public void GetNotificationsRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new GetNotificationsRequest(true, 2, 10);

        request.UnreadOnly.ShouldBeTrue();
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(10);
    }

    [Fact]
    public void GetNotificationsRequest_IsRecord_EqualityWorks()
    {
        var request1 = new GetNotificationsRequest(true, 1, 20);
        var request2 = new GetNotificationsRequest(true, 1, 20);
        var request3 = new GetNotificationsRequest(false, 1, 20);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
