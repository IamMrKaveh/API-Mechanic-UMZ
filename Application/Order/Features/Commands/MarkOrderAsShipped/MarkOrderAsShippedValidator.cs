namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedValidator : AbstractValidator<MarkOrderAsShippedCommand>
{
    public MarkOrderAsShippedValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}