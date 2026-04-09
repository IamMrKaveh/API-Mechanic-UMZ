namespace Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.CategoryName)
            .NotEmpty().WithMessage("Category name is required.");
    }
}