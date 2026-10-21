using Application.Common.Validation;

namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
{

    public DeleteCategoryValidator()
    {
        this.RuleForRequiredId(x => x.CategoryId);
    }
}
