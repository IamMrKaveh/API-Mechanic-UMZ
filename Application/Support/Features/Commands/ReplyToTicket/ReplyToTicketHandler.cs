using Domain.Common.Exceptions;
using Domain.Support.Enums;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Commands.ReplyToTicket;

public class ReplyToTicketHandler(
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ReplyToTicketCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReplyToTicketCommand request, CancellationToken ct)
    {
        var ticketId = TicketId.From(request.TicketId);
        var senderId = UserId.From(request.SenderId);

        var ticket = await ticketRepository.GetByIdWithMessagesAsync(ticketId, ct);
        if (ticket is null)
            return ServiceResult.NotFound("تیکت یافت نشد.");

        if (!request.IsAdmin && ticket.CustomerId != senderId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        try
        {
            var messageId = TicketMessageId.NewId();
            var senderType = request.IsAdmin
                ? TicketMessageSenderType.Agent
                : TicketMessageSenderType.Customer;

            ticket.AddMessage(messageId, senderId, senderType, request.Content);

            ticketRepository.Update(ticket);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}