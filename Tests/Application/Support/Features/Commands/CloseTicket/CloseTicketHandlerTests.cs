using Application.Common.Interfaces;
using Application.Support.Features.Commands.CloseTicket;
using Domain.Support.Aggregates;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Support.Features.Commands.CloseTicket;

public class CloseTicketHandlerTests : HandlerTestBase
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>(); private readonly CloseTicketHandler _sut; private readonly Guid _userGuid = Guid.NewGuid();

    public CloseTicketHandlerTests()
    {
        CurrentUserService.UserId.Returns((Guid?)_userGuid);
        _sut = new CloseTicketHandler(_ticketRepository, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ReturnsNotFound()
    {
        _ticketRepository
            .GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        var command = new CloseTicketCommand(Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _ticketRepository.DidNotReceive().Update(Arg.Any<Ticket>());
    }

    [Fact]
    public async Task Handle_WhenNonAdminAndNotOwner_ReturnsForbidden()
    {
        var otherOwner = UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(otherOwner).Build();

        CurrentUserService.IsAdmin.Returns(false);
        _ticketRepository
            .GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var command = new CloseTicketCommand(ticket.Id.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _ticketRepository.DidNotReceive().Update(Arg.Any<Ticket>());
    }

    [Fact]
    public async Task Handle_WhenNonAdminOwner_ClosesTicketAndReturnsSuccess()
    {
        var customerId = UserId.From(_userGuid);
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();

        CurrentUserService.IsAdmin.Returns(false);
        _ticketRepository
            .GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var command = new CloseTicketCommand(ticket.Id.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        ticket.Status.ShouldBe(TicketStatus.Closed);
        ticket.IsClosed.ShouldBeTrue();
        _ticketRepository.Received(1).Update(ticket);
    }

    [Fact]
    public async Task Handle_WhenAdminAndNotOwner_ClosesTicketAndReturnsSuccess()
    {
        var otherOwner = UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(otherOwner).Build();

        CurrentUserService.IsAdmin.Returns(true);
        _ticketRepository
            .GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var command = new CloseTicketCommand(ticket.Id.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        ticket.Status.ShouldBe(TicketStatus.Closed);
        _ticketRepository.Received(1).Update(ticket);
    }
}
