namespace Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام دسته‌بندی الزامی است.")
            .MaximumLength(100).WithMessage("نام دسته‌بندی نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.IconFile)
            .Must(f => f == null || f.Length < 2 * 1024 * 1024)
            .WithMessage("حجم فایل باید کمتر از ۲ مگابایت باشد.");
    }
}