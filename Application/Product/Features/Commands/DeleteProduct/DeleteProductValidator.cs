namespace Application.Product.Features.Commands.DeleteProduct;

public sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("شناسه محصول الزامی است.");

        RuleFor(x => x.DeletedByUserId)
            .NotEmpty().WithMessage("کاربر حذف‌کننده الزامی است.");
    }
}