namespace Application.Search.Features.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsValidator : AbstractValidator<GetSearchSuggestionsQuery>
{
    public GetSearchSuggestionsValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty().WithMessage("عبارت جستجو نمی‌تواند خالی باشد.")
            .MinimumLength(2).WithMessage("عبارت جستجو باید حداقل ۲ کاراکتر باشد.");

        RuleFor(x => x.MaxSuggestions)
            .InclusiveBetween(1, 20)
            .WithMessage("تعداد پیشنهادات باید بین ۱ تا ۲۰ باشد.");
    }
}