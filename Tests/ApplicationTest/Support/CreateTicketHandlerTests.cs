using Domain.Support.Aggregates;
using Domain.Support.Interfaces;

namespace Tests.ApplicationTest.Support;

public class CreateTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTicketHandler> _logger;
    private readonly CreateTicketHandler _handler;

    public CreateTicketHandlerTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<CreateTicketHandler>>();
        _handler = new CreateTicketHandler(_ticketRepository, _unitOfWork, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateTicketAndReturnSuccess()
    {
        var command = new CreateTicketCommand(
            UserId: 1,
            Subject: "مشکل در ثبت سفارش",
            Priority: Ticket.TicketPriorities.Normal,
            Message: "لطفاً مشکل را بررسی کنید.");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallAddAsync()
    {
        var command = new CreateTicketCommand(1, "موضوع تیکت تست", Ticket.TicketPriorities.Normal, "پیام اول تیکت");

        await _handler.Handle(command, CancellationToken.None);

        await _ticketRepository.Received(1).AddAsync(
            Arg.Any<Ticket>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallSaveChanges()
    {
        var command = new CreateTicketCommand(1, "موضوع تیکت تست", Ticket.TicketPriorities.Normal, "پیام");

        await _handler.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectTicketToRepository()
    {
        Ticket? capturedTicket = null;
        await _ticketRepository.AddAsync(
            Arg.Do<Ticket>(t => capturedTicket = t),
            Arg.Any<CancellationToken>());

        var command = new CreateTicketCommand(5, "موضوع تیکت", Ticket.TicketPriorities.High, "پیام اول");

        await _handler.Handle(command, CancellationToken.None);

        capturedTicket.Should().NotBeNull();
        capturedTicket!.UserId.Should().Be(5);
        capturedTicket.Priority.Should().Be(Ticket.TicketPriorities.High);
        capturedTicket.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnTicketId()
    {
        var command = new CreateTicketCommand(1, "موضوع تیکت", Ticket.TicketPriorities.Normal, "پیام");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Value.Should().BeGreaterThanOrEqualTo(0);
    }
}