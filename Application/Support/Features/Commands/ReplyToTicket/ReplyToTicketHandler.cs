using Domain.Support.Enums;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Support.Features.Commands.ReplyToTicket;

public class ReplyToTicketHandler(
    ITicketRepository ticketRepository,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReplyToTicketCommand>
{
    public async Task<ServiceResult> Handle(ReplyToTicketCommand request, CancellationToken ct)
    {
        var ticketId = TicketId.From(request.TicketId);
        var senderId = UserId.From(currentUser.UserId!.Value);

        var ticket = await ticketRepository.GetByIdWithMessagesAsync(ticketId, ct);
        if (ticket is null)
            return ServiceResult.NotFound("تیکت یافت نشد.");

        if (!currentUser.IsAdmin && ticket.CustomerId != senderId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        try
        {
            var messageId = TicketMessageId.NewId();
            var senderType = currentUser.IsAdmin
                ? TicketMessageSenderType.Agent
                : TicketMessageSenderType.Customer;

            ticket.AddMessage(messageId, senderId, senderType, request.Content, dateTimeProvider.UtcNow);

            ticketRepository.Update(ticket);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}