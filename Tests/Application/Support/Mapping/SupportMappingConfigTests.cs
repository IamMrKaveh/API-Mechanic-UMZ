using Application.Support.Features.Shared;
using Application.Support.Mapping;
using Domain.Support.Entities;
using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Mapster;

namespace Tests.Application.Support.Mapping;

public class SupportMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public SupportMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new SupportMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Ticket_ToTicketDto_MapsIdentityCategoryPriorityStatus()
    {
        var ticket = new TicketBuilder()
            .WithSubject("Broken brake pad")
            .WithCategoryValue("Product")
            .Build();

        var dto = _mapper.Map<TicketDto>(ticket);

        dto.Id.ShouldBe(ticket.Id.Value);
        dto.UserId.ShouldBe(ticket.CustomerId.Value);
        dto.CustomerId.ShouldBe(ticket.CustomerId.Value);
        dto.AssignedAgentId.ShouldBeNull();
        dto.Subject.ShouldBe("Broken brake pad");
        dto.Category.ShouldBe(ticket.Category.Value);
        dto.Priority.ShouldBe(ticket.Priority.Value);
        dto.PriorityDisplayName.ShouldBe(ticket.Priority.DisplayName);
        dto.Status.ShouldBe(ticket.Status.Value);
        dto.StatusDisplayName.ShouldBe(ticket.Status.DisplayName);
        dto.MessageCount.ShouldBe(0);
        dto.CreatedAt.ShouldBe(ticket.CreatedAt);
        dto.UpdatedAt.ShouldBe(ticket.UpdatedAt);
        dto.LastActivityAt.ShouldBe(ticket.LastActivityAt);
        dto.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void Map_Ticket_ToListItemDto_MapsLastReplyFromActivity()
    {
        var ticket = new TicketBuilder().Build();

        var dto = _mapper.Map<TicketListItemDto>(ticket);

        dto.Id.ShouldBe(ticket.Id.Value);
        dto.Subject.ShouldBe(ticket.Subject);
        dto.LastReplyAt.ShouldBe(ticket.LastActivityAt);
    }

    [Fact]
    public void Map_TicketMessage_ToDto_MapsAgentFlag()
    {
        var ticket = new TicketBuilder().Build();
        var agentId = UserId.NewId();
        var message = ticket.AddMessage(
            TicketMessageId.NewId(), agentId,
            TicketMessageSenderType.Agent, "We will replace it", DateTime.UtcNow);

        var dto = _mapper.Map<TicketMessageDto>(message);

        dto.Id.ShouldBe(message.Id.Value);
        dto.TicketId.ShouldBe(ticket.Id.Value);
        dto.SenderId.ShouldBe(agentId.Value);
        dto.SenderType.ShouldBe(TicketMessageSenderType.Agent.ToString());
        dto.Content.ShouldBe("We will replace it");
        dto.IsAdminReply.ShouldBeTrue();
        dto.CreatedAt.ShouldBe(message.SentAt);
    }

    [Fact]
    public void Map_CustomerMessage_IsNotAdminReply()
    {
        var ticket = new TicketBuilder().Build();
        var message = ticket.AddMessage(
            TicketMessageId.NewId(), ticket.CustomerId,
            TicketMessageSenderType.Customer, "My part broke", DateTime.UtcNow);

        var dto = _mapper.Map<TicketMessageDto>(message);

        dto.IsAdminReply.ShouldBeFalse();
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new SupportMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
