namespace Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام دسته‌بندی الزامی است.")
            .MaximumLength(100).WithMessage("نام دسته‌بندی نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).When(x => x.Slug is not null);
    }
}