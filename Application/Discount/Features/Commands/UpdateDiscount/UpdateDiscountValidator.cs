namespace Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountValidator : AbstractValidator<UpdateDiscountCommand>
{
    public UpdateDiscountValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}