using Application.Common.Interfaces;
using Application.Support.Features.Commands.ReplyToTicket;
using Domain.Support.Aggregates;
using Domain.Support.Enums;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Support.Features.Commands.ReplyToTicket;

public class ReplyToTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>(); private readonly ReplyToTicketHandler _sut; private readonly Guid _userGuid = Guid.NewGuid(); private readonly DateTime _now = new(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    public ReplyToTicketHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)_userGuid);
        _dateTimeProvider.UtcNow.Returns(_now);
        _sut = new ReplyToTicketHandler(_ticketRepository, _currentUserService, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ReturnsNotFound()
    {
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        var command = new ReplyToTicketCommand(Guid.NewGuid(), "Hello");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _ticketRepository.DidNotReceive().Update(Arg.Any<Ticket>());
    }

    [Fact]
    public async Task Handle_WhenNonAdminAndNotOwner_ReturnsForbidden()
    {
        var otherOwner = UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(otherOwner).Build();

        _currentUserService.IsAdmin.Returns(false);
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var command = new ReplyToTicketCommand(ticket.Id.Value, "Hello");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _ticketRepository.DidNotReceive().Update(Arg.Any<Ticket>());
    }

    [Fact]
    public async Task Handle_WhenNonAdminOwner_AddsCustomerMessageAndReturnsSuccess()
    {
        var customerId = UserId.From(_userGuid);
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();

        _currentUserService.IsAdmin.Returns(false);
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var command = new ReplyToTicketCommand(ticket.Id.Value, "Hello");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        ticket.Messages.Count.ShouldBe(1);
        var message = ticket.Messages.Single();
        message.SenderType.ShouldBe(TicketMessageSenderType.Customer);
        message.SenderId.ShouldBe(customerId);
        message.Content.ShouldBe("Hello");
        message.SentAt.ShouldBe(_now);
        _ticketRepository.Received(1).Update(ticket);
    }

    [Fact]
    public async Task Handle_WhenAdmin_AddsAgentMessageAndReturnsSuccess()
    {
        var customerId = UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();

        _currentUserService.IsAdmin.Returns(true);
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var command = new ReplyToTicketCommand(ticket.Id.Value, "Agent reply");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        ticket.Messages.Count.ShouldBe(1);
        var message = ticket.Messages.Single();
        message.SenderType.ShouldBe(TicketMessageSenderType.Agent);
        message.SenderId.Value.ShouldBe(_userGuid);
        message.Content.ShouldBe("Agent reply");
        message.SentAt.ShouldBe(_now);
        ticket.Status.ShouldBe(TicketStatus.AwaitingReply);
        _ticketRepository.Received(1).Update(ticket);
    }

    [Fact]
    public async Task Handle_WhenTicketIsClosed_ReturnsFailureAndDoesNotUpdate()
    {
        var customerId = UserId.From(_userGuid);
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();
        ticket.Close();

        _currentUserService.IsAdmin.Returns(false);
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var command = new ReplyToTicketCommand(ticket.Id.Value, "Hello");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        ticket.Messages.ShouldBeEmpty();
        _ticketRepository.DidNotReceive().Update(Arg.Any<Ticket>());
    }
}
