namespace Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام دسته‌بندی الزامی است.")
            .MaximumLength(100).WithMessage("نام دسته‌بندی نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");
        RuleFor(x => x.Slug)
            .MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("نسخه سطر برای کنترل همزمانی الزامی است.")
            .Must(BeValidBase64).WithMessage("نسخه سطر نامعتبر است.")
            .When(x => !string.IsNullOrWhiteSpace(x.RowVersion));
    }

    private static bool BeValidBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var buffer = new Span<byte>(new byte[value.Length]);
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}