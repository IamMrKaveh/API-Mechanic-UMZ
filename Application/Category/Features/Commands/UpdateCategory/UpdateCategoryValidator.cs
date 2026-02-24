namespace Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام دسته‌بندی الزامی است.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);

        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion الزامی است.");

        RuleFor(x => x.IconFile)
            .Must(f => f == null || f.Length < 2 * 1024 * 1024)
            .WithMessage("حجم فایل باید کمتر از ۲ مگابایت باشد.");
    }
}