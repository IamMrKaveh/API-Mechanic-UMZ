namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnValidator : AbstractValidator<RequestReturnCommand>
{
    public RequestReturnValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("OrderId is required.");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("UserId is required.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("دلیل درخواست بازگشت الزامی است.").MaximumLength(1000);
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required for concurrency control.");
    }
}