namespace Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusValidator : AbstractValidator<CreateOrderStatusCommand>
{
    public CreateOrderStatusValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("نام وضعیت الزامی است.").MaximumLength(50);
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("نام نمایشی وضعیت الزامی است.").MaximumLength(100);
    }
}