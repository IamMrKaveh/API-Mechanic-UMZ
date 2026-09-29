using System.Linq.Expressions;

namespace Application.Common.Validation;

/// <summary>
/// قوانین اعتبارسنجی مشترک FluentValidation برای جلوگیری از کپی-پیست
/// قانون‌های Page/PageSize، بازه تاریخ، شناسه الزامی و شماره موبایل ایرانی.
/// </summary>
public static class ValidatorExtensions
{
    /// <summary>سقف پیش‌فرض اندازه صفحه.</summary>
    public const int DefaultMaxPageSize = 100;

    /// <summary>موبایل ایرانی به فرم استاندارد ۰۹xxxxxxxxx.</summary>
    public const string IranianMobileNumberPattern = @"^09\d{9}$";

    /// <summary>شماره تلفن ایرانی با پیش‌شماره‌های اختیاری ‎+98/0098/98/0.</summary>
    public const string IranianPhoneNumberPattern = @"^(\+98|0098|98|0)?9\d{9}$";

    // —— ۸. صفحه‌بندی ——

    public static IRuleBuilderOptions<T, int> RuleForPage<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, int>> page,
        string message = "شماره صفحه باید بزرگ‌تر از صفر باشد.") =>
        validator.RuleFor(page)
            .GreaterThan(0)
            .WithMessage(message);

    public static IRuleBuilderOptions<T, int> RuleForPageSize<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, int>> pageSize,
        int maxPageSize = DefaultMaxPageSize,
        string? message = null) =>
        validator.RuleFor(pageSize)
            .InclusiveBetween(1, maxPageSize)
            .WithMessage(message ?? $"اندازه صفحه باید بین ۱ تا {ToPersianDigits(maxPageSize)} باشد.");

    public static void RuleForPagination<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, int>> page,
        Expression<Func<T, int>> pageSize,
        int maxPageSize = DefaultMaxPageSize,
        string pageMessage = "شماره صفحه باید بزرگ‌تر از صفر باشد.",
        string? pageSizeMessage = null)
    {
        validator.RuleForPage(page, pageMessage);
        validator.RuleForPageSize(pageSize, maxPageSize, pageSizeMessage);
    }

    private static string ToPersianDigits(int value) =>
        value.ToString(CultureInfo.InvariantCulture)
            .Replace('0', '۰')
            .Replace('1', '۱')
            .Replace('2', '۲')
            .Replace('3', '۳')
            .Replace('4', '۴')
            .Replace('5', '۵')
            .Replace('6', '۶')
            .Replace('7', '۷')
            .Replace('8', '۸')
            .Replace('9', '۹');

    // —— ۹. بازه تاریخ ——

    /// <summary>
    /// بازه تاریخ اختیاری روی جفت <see cref="DateTime?"/> (شکل Analytics).
    /// </summary>
    public static void RuleForOptionalDateRange<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, DateTime?>> from,
        Expression<Func<T, DateTime?>> to,
        bool allowEqual = true,
        bool ruleOnFromProperty = true,
        string message = "تاریخ شروع باید قبل از تاریخ پایان باشد.")
    {
        var fromSelector = from.Compile();
        var toSelector = to.Compile();

        validator.When(x => fromSelector(x).HasValue && toSelector(x).HasValue, () =>
        {
            if (ruleOnFromProperty)
            {
                var rule = allowEqual
                    ? validator.RuleFor(from).LessThanOrEqualTo(to)
                    : validator.RuleFor(from).LessThan(to);
                rule.WithMessage(message);
            }
            else
            {
                var rule = allowEqual
                    ? validator.RuleFor(to).GreaterThanOrEqualTo(from)
                    : validator.RuleFor(to).GreaterThan(from);
                rule.WithMessage(message);
            }
        });
    }

    /// <summary>
    /// بازه تاریخ اختیاری روی جفت Value (شکل Wallet/Export)؛ نام property
    /// (مثل FromDate.Value) و پیام موجود حفظ می‌شود.
    /// </summary>
    public static void RuleForOptionalDateRangeValues<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, DateTime?>> from,
        Expression<Func<T, DateTime?>> to,
        Expression<Func<T, DateTime>> fromValue,
        Expression<Func<T, DateTime>> toValue,
        bool allowEqual = true,
        string? message = null)
    {
        var fromSelector = from.Compile();
        var toSelector = to.Compile();

        validator.When(x => fromSelector(x).HasValue && toSelector(x).HasValue, () =>
        {
            var rule = allowEqual
                ? validator.RuleFor(fromValue).LessThanOrEqualTo(toValue)
                : validator.RuleFor(fromValue).LessThan(toValue);

            if (message is not null)
                rule.WithMessage(message);
        });
    }

    /// <summary>
    /// بازه تاریخ در سطح مدل (شکل Audit/Review/Overview)؛ با name اختیاری
    /// برای حفظ PropertyName مورد انتظار تست‌ها (مثل DateRange).
    /// </summary>
    public static void RuleForDateRange<T>(
        this AbstractValidator<T> validator,
        Func<T, DateTime?> from,
        Func<T, DateTime?> to,
        string message,
        string? name = null)
    {
        var rule = validator.RuleFor(x => x)
            .Must(x =>
            {
                var f = from(x);
                var t = to(x);
                return f is null || t is null || f.Value <= t.Value;
            })
            .WithMessage(message);

        if (name is not null)
            rule.WithName(name);
    }

    /// <summary>
    /// بازه تاریخ اجباری با سقف آینده برای ToDate (شکل RevenueReport/SalesChartData).
    /// </summary>
    public static void RuleForRequiredDateRange<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, DateTime>> from,
        Expression<Func<T, DateTime>> to,
        Func<DateTime> utcNow,
        string fromRequiredMessage = "تاریخ شروع الزامی است.",
        string orderMessage = "تاریخ شروع باید قبل از تاریخ پایان باشد.",
        string toRequiredMessage = "تاریخ پایان الزامی است.",
        string futureMessage = "تاریخ پایان نمی‌تواند در آینده باشد.")
    {
        validator.RuleFor(from)
            .NotEmpty().WithMessage(fromRequiredMessage)
            .LessThan(to).WithMessage(orderMessage);

        validator.RuleFor(to)
            .NotEmpty().WithMessage(toRequiredMessage)
            .LessThanOrEqualTo(_ => utcNow().AddDays(1)).WithMessage(futureMessage);
    }

    // —— ۱۰. شناسه الزامی ——

    public static IRuleBuilderOptions<T, Guid> RuleForRequiredId<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, Guid>> id,
        string message = "شناسه الزامی است.") =>
        validator.RuleFor(id).RequiredId(message);

    public static IRuleBuilderOptions<T, Guid> RequiredId<T>(
        this IRuleBuilder<T, Guid> rule,
        string message = "شناسه الزامی است.") =>
        rule.NotEmpty().WithMessage(message);

    public static void RuleForEachRequiredId<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, IEnumerable<Guid>>> ids,
        string message = "شناسه نامعتبر است.") =>
        validator.RuleForEach(ids).NotEmpty().WithMessage(message);

    // —— ۱۱. شماره موبایل ایرانی ——

    /// <summary>
    /// موبایل به فرم استاندارد ۰۹xxxxxxxxx (شکل SendOtp/VerifyOtp).
    /// </summary>
    public static IRuleBuilderOptions<T, string> RuleForIranianMobileNumber<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string>> phoneNumber,
        string requiredMessage = "شماره موبایل الزامی است.",
        string invalidMessage = "فرمت شماره موبایل نامعتبر است.") =>
        validator.RuleFor(phoneNumber)
            .NotEmpty().WithMessage(requiredMessage)
            .Matches(IranianMobileNumberPattern).WithMessage(invalidMessage);

    /// <summary>
    /// شماره تلفن با پیش‌شماره اختیاری (شکل ChangePhoneNumber، هم‌راستا با PhoneNumber دامین).
    /// </summary>
    public static IRuleBuilderOptions<T, string> RuleForIranianPhoneNumber<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string>> phoneNumber,
        string requiredMessage = "شماره تلفن جدید الزامی است.",
        string invalidMessage = "فرمت شماره تلفن نامعتبر است.") =>
        validator.RuleFor(phoneNumber)
            .NotEmpty().WithMessage(requiredMessage)
            .Matches(IranianPhoneNumberPattern).WithMessage(invalidMessage);
}
