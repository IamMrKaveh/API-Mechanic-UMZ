namespace Application.Support.Features.Commands.CloseTicket;

public sealed class CloseTicketValidator : AbstractValidator<CloseTicketCommand>
{
    public CloseTicketValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0).WithMessage("شناسه تیکت الزامی است.");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("شناسه کاربر الزامی است.");
    }
}