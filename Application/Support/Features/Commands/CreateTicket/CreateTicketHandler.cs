using Application.Support.Features.Shared;
using Domain.Support.Aggregates;
using Domain.Support.Enums;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Support.Features.Commands.CreateTicket;

public class CreateTicketHandler(
    ITicketRepository ticketRepository,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IMapper mapper)
    : ICommandHandler<CreateTicketCommand, TicketDto>
{
    public async Task<ServiceResult<TicketDto>> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        var category = TicketCategory.Create(request.Category);
        var priority = string.IsNullOrWhiteSpace(request.Priority)
            ? TicketPriority.Normal
            : TicketPriority.FromString(request.Priority);

        var ticketId = TicketId.NewId();
        var customerId = UserId.From(currentUser.UserId!.Value);
        var now = dateTimeProvider.UtcNow;

        var ticket = Ticket.Open(ticketId, customerId, request.Subject, category, priority);

        var messageId = TicketMessageId.NewId();
        ticket.AddMessage(messageId, customerId, TicketMessageSenderType.Customer, request.Message, now);

        await ticketRepository.AddAsync(ticket, ct);

        return ServiceResult<TicketDto>.Success(mapper.Map<TicketDto>(ticket));
    }
}