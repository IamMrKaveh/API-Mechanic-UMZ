namespace Application.Support.Features.Commands.ReplyToTicket;

public class ReplyToTicketValidator : AbstractValidator<ReplyToTicketCommand>
{
    public ReplyToTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.SenderId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}