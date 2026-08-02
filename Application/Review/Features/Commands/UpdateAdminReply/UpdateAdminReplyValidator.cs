namespace Application.Review.Features.Commands.UpdateAdminReply;

public sealed class UpdateAdminReplyValidator : AbstractValidator<UpdateAdminReplyCommand>
{
    public UpdateAdminReplyValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Reply)
            .NotEmpty().WithMessage("متن پاسخ الزامی است.")
            .MaximumLength(1000).WithMessage("متن پاسخ نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
    }
}
