using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;

namespace Application.Support.Features.Commands.CloseTicket;

public class CloseTicketHandler(
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CloseTicketCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CloseTicketCommand request, CancellationToken ct)
    {
        var ticket = await ticketRepository.GetByIdAsync(TicketId.From(request.TicketId), ct);
        if (ticket is null)
            return ServiceResult.NotFound("تیکت یافت نشد.");

        if (!request.IsAdmin && ticket.CustomerId.Value != request.UserId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        ticket.Close();
        ticketRepository.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}