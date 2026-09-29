using Application.Common.Validation;

namespace Application.Review.Features.Commands.CreateReview;

public class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        this.RuleForRequiredId(x => x.ProductId);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("امتیاز باید بین ۱ تا ۵ باشد.");
        RuleFor(x => x.Title).MaximumLength(100).When(x => x.Title is not null);
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment is not null);
    }
}