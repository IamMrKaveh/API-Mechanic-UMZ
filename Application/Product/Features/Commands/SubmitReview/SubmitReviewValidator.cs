namespace Application.Product.Features.Commands.SubmitReview;

public class SubmitReviewValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("شناسه محصول نامعتبر است.");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("شناسه کاربر نامعتبر است.");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("امتیاز باید بین ۱ تا ۵ باشد.");
        RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان نباید بیش از ۱۰۰ کاراکتر باشد.");
        RuleFor(x => x.Comment).MaximumLength(1000).WithMessage("متن نظر نباید بیش از ۱۰۰۰ کاراکتر باشد.");
    }
}