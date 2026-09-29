using Application.Common.Validation;

namespace Application.Product.Features.Commands.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        this.RuleForRequiredId(x => x.Id, "شناسه محصول الزامی است.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام محصول الزامی است.")
            .MaximumLength(200).WithMessage("نام محصول نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("دسته‌بندی الزامی است.");

        RuleFor(x => x.BrandId)
            .NotEmpty().WithMessage("برند الزامی است.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion الزامی است.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")
            .When(x => x.Slug is not null);
    }
}