namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedValidator : AbstractValidator<MarkOrderAsShippedCommand>
{
    public MarkOrderAsShippedValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("OrderId is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required for concurrency control.");
    }
}