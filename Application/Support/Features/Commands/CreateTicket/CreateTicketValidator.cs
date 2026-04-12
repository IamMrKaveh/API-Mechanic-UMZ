namespace Application.Support.Features.Commands.CreateTicket;

public class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
    }
}