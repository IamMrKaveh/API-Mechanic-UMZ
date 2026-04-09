namespace Application.Search.Features.Queries.FuzzySearch;

public class FuzzySearchValidator : AbstractValidator<FuzzySearchQuery>
{
    public FuzzySearchValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty().WithMessage("عبارت جستجو نمی‌تواند خالی باشد.");
    }
}