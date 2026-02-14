namespace Application.Categories.Features.Commands.UpdateCategoryGroup;

public class UpdateCategoryGroupValidator : AbstractValidator<UpdateCategoryGroupCommand>
{
    public UpdateCategoryGroupValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("شناسه دسته‌بندی الزامی است.");
        RuleFor(x => x.GroupId).GreaterThan(0).WithMessage("شناسه گروه الزامی است.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام گروه الزامی است.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion الزامی است.");

        RuleFor(x => x.IconFile)
            .Must(f => f == null || f.Length < 2 * 1024 * 1024)
            .WithMessage("حجم فایل باید کمتر از ۲ مگابایت باشد.");
    }
}