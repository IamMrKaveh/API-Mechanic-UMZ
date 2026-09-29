using Application.Common.Validation;

namespace Application.Order.Features.Commands.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(256);
        this.RuleForRequiredId(x => x.AdminUserId);
        this.RuleForRequiredId(x => x.UserId);
        this.RuleForRequiredId(x => x.UserAddressId);
        this.RuleForRequiredId(x => x.ShippingId);
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OrderItems).NotEmpty().WithMessage("سفارش باید حداقل یک آیتم داشته باشد.");
    }
}