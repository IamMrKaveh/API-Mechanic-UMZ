namespace Application.Media.Features.Commands.UploadMedia;

public class UploadMediaValidator : AbstractValidator<UploadMediaCommand>
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxFileSize = 10 * 1024 * 1024;

    public UploadMediaValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).Must(ct => AllowedTypes.Contains(ct))
            .WithMessage("فرمت فایل مجاز نیست.");
        RuleFor(x => x.FileSize).LessThanOrEqualTo(MaxFileSize)
            .WithMessage("حجم فایل نمی‌تواند بیش از ۱۰ مگابایت باشد.");
        RuleFor(x => x.EntityType).NotEmpty();
        RuleFor(x => x.EntityId).NotEmpty();
    }
}