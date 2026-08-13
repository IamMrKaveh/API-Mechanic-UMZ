using Application.Common.Interfaces;
using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Shared;
using Domain.Support.Aggregates;
using Domain.Support.Enums;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Support.Features.Commands.CreateTicket;

public class CreateTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly CreateTicketHandler _sut; private readonly Guid _userGuid = Guid.NewGuid(); private readonly DateTime _now = new(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    public CreateTicketHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)_userGuid);
        _dateTimeProvider.UtcNow.Returns(_now);
        _mapper.Map<TicketDto>(Arg.Any<Ticket>())
            .Returns(ci => new TicketDto
            {
                Id = ((Ticket)ci[0]!).Id.Value,
                CustomerId = ((Ticket)ci[0]!).CustomerId.Value,
                Subject = ((Ticket)ci[0]!).Subject
            });

        _sut = new CreateTicketHandler(_ticketRepository, _currentUserService, _dateTimeProvider, _mapper);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsTicketAndReturnsSuccessWithDto()
    {
        var command = new CreateTicketCommand(
            Subject: "Order help",
            Category: "Billing",
            Priority: "High",
            Message: "Please assist");

        Ticket? added = null;
        await _ticketRepository
            .AddAsync(Arg.Do<Ticket>(t => added = t), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _ticketRepository.Received(1).AddAsync(Arg.Any<Ticket>(), Arg.Any<CancellationToken>());
        added.ShouldNotBeNull();
        added!.Subject.ShouldBe("Order help");
        added.Category.Value.ShouldBe("Billing");
        added.Priority.ShouldBe(TicketPriority.High);
        added.CustomerId.Value.ShouldBe(_userGuid);
        added.Messages.Count.ShouldBe(1);
        var message = added.Messages.Single();
        message.SenderType.ShouldBe(TicketMessageSenderType.Customer);
        message.SenderId.Value.ShouldBe(_userGuid);
        message.Content.ShouldBe("Please assist");
        message.SentAt.ShouldBe(_now);
        result.Value.Id.ShouldBe(added.Id.Value);
        result.Value.Subject.ShouldBe("Order help");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenPriorityIsMissing_UsesNormalPriority(string? priority)
    {
        var command = new CreateTicketCommand("Subject", "Billing", priority, "Message");

        Ticket? added = null;
        await _ticketRepository
            .AddAsync(Arg.Do<Ticket>(t => added = t), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        added.ShouldNotBeNull();
        added!.Priority.ShouldBe(TicketPriority.Normal);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Normal")]
    [InlineData("High")]
    [InlineData("Urgent")]
    public async Task Handle_WhenPriorityIsSupplied_UsesParsedPriority(string priority)
    {
        var command = new CreateTicketCommand("Subject", "Billing", priority, "Message");

        Ticket? added = null;
        await _ticketRepository
            .AddAsync(Arg.Do<Ticket>(t => added = t), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        added.ShouldNotBeNull();
        added!.Priority.Value.ShouldBe(priority);
    }

    [Fact]
    public async Task Handle_WhenPriorityIsUnknown_FallsBackToNormalPriority()
    {
        var command = new CreateTicketCommand("Subject", "Billing", "unknown-priority", "Message");

        Ticket? added = null;
        await _ticketRepository
            .AddAsync(Arg.Do<Ticket>(t => added = t), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        added.ShouldNotBeNull();
        added!.Priority.ShouldBe(TicketPriority.Normal);
    }

    [Fact]
    public async Task Handle_WithValidCommand_InitializesTicketAsOpenWithCustomerAsSender()
    {
        var command = new CreateTicketCommand("Subject", "Billing", "Normal", "Message");

        Ticket? added = null;
        await _ticketRepository
            .AddAsync(Arg.Do<Ticket>(t => added = t), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        added.ShouldNotBeNull();
        added!.Status.ShouldBe(TicketStatus.Open);
        added.IsOpen.ShouldBeTrue();
        added.Messages.Single().SenderType.ShouldBe(TicketMessageSenderType.Customer);
    }
}
