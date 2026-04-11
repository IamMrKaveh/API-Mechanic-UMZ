namespace Application.Brand.Features.Commands.CreateBrand;

public sealed class CreateBrandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام برند الزامی است.")
            .MaximumLength(100).WithMessage("نام برند نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("دسته‌بندی الزامی است.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).When(x => x.Slug is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null);
    }
}