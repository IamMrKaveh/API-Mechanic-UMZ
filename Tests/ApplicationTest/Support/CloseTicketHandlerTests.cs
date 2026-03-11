using Domain.Support.Aggregates;
using Domain.Support.Interfaces;

namespace Tests.ApplicationTest.Support;

public class CloseTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CloseTicketHandler> _logger;
    private readonly CloseTicketHandler _handler;

    public CloseTicketHandlerTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<CloseTicketHandler>>();
        _handler = new CloseTicketHandler(_ticketRepository, _unitOfWork, _logger);
    }

    [Fact]
    public async Task Handle_WhenTicketExists_ShouldCloseTicketAndReturnSuccess()
    {
        var ticket = new TicketBuilder().Build();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var result = await _handler.Handle(new CloseTicketCommand(1, 1), CancellationToken.None);

        result.IsSucceed.Should().BeTrue();
        ticket.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ShouldReturnFailure()
    {
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        var result = await _handler.Handle(new CloseTicketCommand(99, 1), CancellationToken.None);

        result.IsSucceed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTicketExists_ShouldCallSaveChanges()
    {
        var ticket = new TicketBuilder().Build();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        await _handler.Handle(new CloseTicketCommand(1, 1), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyClosed_ShouldStillSucceed()
    {
        var ticket = new TicketBuilder().BuildClosed();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var result = await _handler.Handle(new CloseTicketCommand(1, 1), CancellationToken.None);

        result.IsSucceed.Should().BeTrue();
    }
}