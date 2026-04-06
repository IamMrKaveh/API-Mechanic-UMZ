namespace Application.Support.Features.Commands.CloseTicket;

public class CloseTicketValidator : AbstractValidator<CloseTicketCommand>
{
    public CloseTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}