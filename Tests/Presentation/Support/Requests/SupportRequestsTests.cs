using Presentation.Support.Requests;

namespace Tests.Presentation.Support.Requests;

public class SupportRequestsTests
{
    [Fact]
    public void CreateTicketRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateTicketRequest("Subject", "Order", "High", "Message");

        request.Subject.ShouldBe("Subject");
        request.Category.ShouldBe("Order");
        request.Priority.ShouldBe("High");
        request.Message.ShouldBe("Message");
    }

    [Fact]
    public void CreateTicketRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreateTicketRequest("S", "C", "P", "M");
        var request2 = new CreateTicketRequest("S", "C", "P", "M");
        var request3 = new CreateTicketRequest("Other", "C", "P", "M");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void ReplyToTicketRequest_WithMessage_SetsCorrectly()
    {
        var request = new ReplyToTicketRequest("Extra info");

        request.Message.ShouldBe("Extra info");
    }

    [Fact]
    public void CloseTicketRequest_WithDefaults_SetsIsAdminToFalse()
    {
        var request = new CloseTicketRequest();

        request.IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void CloseTicketRequest_WithIsAdmin_SetsCorrectly()
    {
        var request = new CloseTicketRequest(true);

        request.IsAdmin.ShouldBeTrue();
    }
}
