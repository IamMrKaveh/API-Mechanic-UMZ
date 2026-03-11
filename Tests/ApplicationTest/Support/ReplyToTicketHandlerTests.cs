using Domain.Support.Aggregates;
using Domain.Support.Interfaces;

namespace Tests.ApplicationTest.Support;

public class ReplyToTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly TicketDomainService _ticketDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReplyToTicketHandler> _logger;
    private readonly ReplyToTicketHandler _handler;

    public ReplyToTicketHandlerTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _ticketDomainService = new TicketDomainService();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<ReplyToTicketHandler>>();
        _handler = new ReplyToTicketHandler(_ticketRepository, _ticketDomainService, _unitOfWork, _logger);
    }

    [Fact]
    public async Task Handle_WhenTicketExistsAndUserHasAccess_ShouldAddMessageAndReturnSuccess()
    {
        var ticket = new TicketBuilder().WithUserId(1).Build();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ticket);

        var command = new ReplyToTicketCommand(TicketId: 1, SenderId: 1, Message: "پاسخ کاربر", IsAdminReply: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.MessageCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenAdminReplies_ShouldSetStatusToAnswered()
    {
        var ticket = new TicketBuilder().WithUserId(1).Build();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ticket);

        var command = new ReplyToTicketCommand(1, 99, "پاسخ ادمین", IsAdminReply: true);

        await _handler.Handle(command, CancellationToken.None);

        ticket.IsAnswered.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ShouldThrowException()
    {
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var command = new ReplyToTicketCommand(99, 1, "پیام", IsAdminReply: false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_WhenUserNotOwner_ShouldThrowException()
    {
        var ticket = new TicketBuilder().WithUserId(1).Build();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ticket);

        var command = new ReplyToTicketCommand(1, SenderId: 99, "پیام", IsAdminReply: false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_WhenTicketClosed_ShouldReturnFailure()
    {
        var ticket = new TicketBuilder().WithUserId(1).BuildClosed();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ticket);

        var command = new ReplyToTicketCommand(1, 1, "پیام", IsAdminReply: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldCallSaveChanges()
    {
        var ticket = new TicketBuilder().WithUserId(1).Build();
        _ticketRepository.GetByIdWithMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ticket);

        var command = new ReplyToTicketCommand(1, 1, "پیام کاربر", IsAdminReply: false);

        await _handler.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}