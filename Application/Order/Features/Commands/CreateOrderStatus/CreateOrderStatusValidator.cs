namespace Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusValidator : AbstractValidator<CreateOrderStatusCommand>
{
    private static readonly System.Text.RegularExpressions.Regex HexColorRegex =
        new("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public CreateOrderStatusValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام وضعیت الزامی است.")
            .MaximumLength(50).WithMessage("نام وضعیت نمی‌تواند بیش از 50 کاراکتر باشد.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("نام نمایشی وضعیت الزامی است.")
            .MaximumLength(100).WithMessage("نام نمایشی نمی‌تواند بیش از 100 کاراکتر باشد.");

        RuleFor(x => x.Icon)
            .MaximumLength(100).WithMessage("نام آیکن نمی‌تواند بیش از 100 کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Icon));

        RuleFor(x => x.Color)
            .MaximumLength(50).WithMessage("کد رنگ نمی‌تواند بیش از 50 کاراکتر باشد.")
            .Must(c => string.IsNullOrWhiteSpace(c) || HexColorRegex.IsMatch(c))
            .WithMessage("کد رنگ باید در قالب Hex معتبر باشد (مثال: #FFAA00).")
            .When(x => !string.IsNullOrWhiteSpace(x.Color));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("ترتیب نمایش نمی‌تواند منفی باشد.");
    }
}