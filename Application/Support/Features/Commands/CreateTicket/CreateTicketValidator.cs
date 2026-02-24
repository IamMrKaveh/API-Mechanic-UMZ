namespace Application.Support.Features.Commands.CreateTicket;

public sealed class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("شناسه کاربر الزامی است.");
        RuleFor(x => x.Subject).NotEmpty().WithMessage("موضوع تیکت الزامی است.")
            .MinimumLength(5).WithMessage("موضوع تیکت باید حداقل ۵ کاراکتر باشد.")
            .MaximumLength(200).WithMessage("موضوع تیکت نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
        RuleFor(x => x.Priority).NotEmpty().WithMessage("اولویت تیکت الزامی است.")
            .Must(p => new[] { "Low", "Normal", "High", "Urgent" }.Contains(p))
            .WithMessage("اولویت نامعتبر است.");
        RuleFor(x => x.Message).NotEmpty().WithMessage("متن پیام الزامی است.")
            .MaximumLength(5000).WithMessage("متن پیام نمی‌تواند بیش از ۵۰۰۰ کاراکتر باشد.");
    }
}