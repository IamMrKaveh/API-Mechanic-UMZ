namespace Application.Media.Features.Commands.UploadMedia;

public class UploadMediaValidator : AbstractValidator<UploadMediaCommand>
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/svg+xml" };
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public UploadMediaValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("نام فایل الزامی است.")
            .MaximumLength(255).WithMessage("نام فایل نمی‌تواند بیش از ۲۵۵ کاراکتر باشد.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("نوع فایل الزامی است.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("حجم فایل باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(MaxFileSize).WithMessage("حجم فایل نمی‌تواند بیش از ۵۰ مگابایت باشد.");

        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("نوع موجودیت الزامی است.")
            .MaximumLength(50).WithMessage("نوع موجودیت نمی‌تواند بیش از ۵۰ کاراکتر باشد.");

        RuleFor(x => x.EntityId)
            .GreaterThan(0).WithMessage("شناسه موجودیت الزامی است.");

        RuleFor(x => x.AltText)
            .MaximumLength(500).WithMessage("متن جایگزین نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("فایل الزامی است.");
    }
}