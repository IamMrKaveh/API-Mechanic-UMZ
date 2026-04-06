using Application.Common.Results;
using Domain.Common.Interfaces;
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
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(TicketId.From(request.TicketId), ct);
        if (ticket is null)
            return ServiceResult.NotFound("تیکت یافت نشد.");

        if (!request.IsAdmin && ticket.CustomerId.Value != request.SenderId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        try
        {
            var messageId = TicketMessageId.NewId();
            var senderId = UserId.From(request.SenderId);
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