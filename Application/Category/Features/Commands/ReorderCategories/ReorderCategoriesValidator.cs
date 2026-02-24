namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesValidator()
    {
        RuleFor(x => x.OrderedCategoryIds)
            .NotNull().WithMessage("لیست شناسه‌ها الزامی است.")
            .Must(ids => ids != null && ids.Count > 0).WithMessage("لیست شناسه‌ها نمی‌تواند خالی باشد.")
            .Must(ids => ids != null && ids.Distinct().Count() == ids.Count)
            .WithMessage("شناسه‌های تکراری مجاز نیست.");
    }
}