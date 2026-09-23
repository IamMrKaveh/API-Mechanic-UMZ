using Application.Support.Features.Shared;

namespace Tests.Application.Support.Features.Shared;

public class TicketDtosTests
{
    [Fact]
    public void TicketDto_Defaults_AreEmpty()
    {
        var dto = new TicketDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Subject.ShouldBe(string.Empty);
        dto.Category.ShouldBe(string.Empty);
        dto.Priority.ShouldBe(string.Empty);
        dto.Status.ShouldBe(string.Empty);
        dto.AssignedAgentId.ShouldBeNull();
        dto.UserFullName.ShouldBeNull();
        dto.Messages.ShouldNotBeNull();
        dto.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void TicketDto_WithMessages_PreservesCollection()
    {
        var dto = new TicketDto
        {
            Id = Guid.NewGuid(),
            Subject = "Broken part",
            Messages = new List<TicketMessageDto>
            {
                new() { Id = Guid.NewGuid(), Content = "Hello", SenderType = "Customer" }
            }
        };

        dto.Messages.Count.ShouldBe(1);
        dto.Messages[0].Content.ShouldBe("Hello");
    }

    [Fact]
    public void TicketListItemDto_RoundTrip()
    {
        var dto = new TicketListItemDto
        {
            Id = Guid.NewGuid(), Subject = "Late delivery", Category = "Shipping",
            Priority = "High", Status = "Open", MessageCount = 3,
            CreatedAt = new DateTime(2026, 2, 1), LastReplyAt = new DateTime(2026, 2, 2)
        };

        dto.MessageCount.ShouldBe(3);
        dto.LastReplyAt.ShouldBe(new DateTime(2026, 2, 2));
    }

    [Fact]
    public void TicketMessageDto_Defaults_AreEmpty()
    {
        var dto = new TicketMessageDto();

        dto.SenderType.ShouldBe(string.Empty);
        dto.Content.ShouldBe(string.Empty);
        dto.SenderName.ShouldBeNull();
        dto.CreatedAt.ShouldBe(default);
    }
}
