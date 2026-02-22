namespace Application.Order.Features.Commands.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(256);
        RuleFor(x => x.AdminUserId).GreaterThan(0);
        RuleFor(x => x.Dto.UserId).GreaterThan(0);
        RuleFor(x => x.Dto.UserAddressId).GreaterThan(0);
        RuleFor(x => x.Dto.ShippingId).GreaterThan(0);
        RuleFor(x => x.Dto.ReceiverName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.OrderItems).NotEmpty().WithMessage("سفارش باید حداقل یک آیتم داشته باشد.");
    }
}