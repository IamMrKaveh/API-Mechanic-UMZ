namespace Application.Order.Features.Commands.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(256);
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserAddressId).NotEmpty();
        RuleFor(x => x.ShippingId).NotEmpty();
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OrderItems).NotEmpty().WithMessage("سفارش باید حداقل یک آیتم داشته باشد.");
    }
}