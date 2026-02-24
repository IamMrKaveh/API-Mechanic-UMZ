namespace Application.Support.Features.Commands.ReplyToTicket;

public sealed class ReplyToTicketValidator : AbstractValidator<ReplyToTicketCommand>
{
    public ReplyToTicketValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0).WithMessage("شناسه تیکت الزامی است.");
        RuleFor(x => x.SenderId).GreaterThan(0).WithMessage("شناسه فرستنده الزامی است.");
        RuleFor(x => x.Message).NotEmpty().WithMessage("متن پیام الزامی است.")
            .MaximumLength(5000).WithMessage("متن پیام نمی‌تواند بیش از ۵۰۰۰ کاراکتر باشد.");
    }
}