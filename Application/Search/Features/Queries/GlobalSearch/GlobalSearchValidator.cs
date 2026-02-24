namespace Application.Search.Features.Queries.GlobalSearch;

public class GlobalSearchValidator : AbstractValidator<GlobalSearchQuery>
{
    public GlobalSearchValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty().WithMessage("عبارت جستجو نمی‌تواند خالی باشد.");
    }
}