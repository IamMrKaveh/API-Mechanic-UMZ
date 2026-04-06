namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("لیست دسته‌بندی‌ها نمی‌تواند خالی باشد.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id).NotEmpty();
            item.RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        });
    }
}