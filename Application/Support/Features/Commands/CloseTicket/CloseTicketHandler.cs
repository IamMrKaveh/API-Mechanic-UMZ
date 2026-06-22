using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Commands.CloseTicket;

public class CloseTicketHandler(
    ITicketRepository ticketRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseTicketCommand>
{
    public async Task<ServiceResult> Handle(CloseTicketCommand request, CancellationToken ct)
    {
        var ticketId = TicketId.From(request.TicketId);
        var userId = UserId.From(currentUser.UserId!.Value);

        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct);
        if (ticket is null)
            return ServiceResult.NotFound("تیکت یافت نشد.");

        if (!currentUser.IsAdmin && ticket.CustomerId != userId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        ticket.Close();
        ticketRepository.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}