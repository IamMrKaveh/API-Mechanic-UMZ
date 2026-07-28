using FluentValidation.Results;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace Presentation.Common.ProblemDetails;

public static class PersianProblemDetailsFactory
{
    private const string TypeBase = "https://ledka.ir/errors/";

    public static PersianProblemDetails FromStatus(
        HttpStatusCode status,
        string? detail,
        string? instance,
        string? traceId,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null)
    {
        var (title, defaultDetail) = TitleAndDetail(status);
        return new PersianProblemDetails
        {
            Type = TypeBase + ((int)status),
            Title = title,
            Status = (int)status,
            Detail = string.IsNullOrWhiteSpace(detail) ? defaultDetail : detail,
            Instance = instance,
            TraceId = traceId,
            ErrorCode = errorCode,
            Errors = errors
        };
    }

    public static PersianProblemDetails FromValidation(
        IEnumerable<ValidationFailure> failures,
        string? instance,
        string? traceId)
    {
        var errors = failures
            .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "_" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new PersianProblemDetails
        {
            Type = TypeBase + 400,
            Title = "اطلاعات ورودی نامعتبر است.",
            Status = 400,
            Detail = "یک یا چند فیلد ورودی معتبر نیست. جزئیات را در بخش errors بررسی کنید.",
            Instance = instance,
            TraceId = traceId,
            ErrorCode = "VALIDATION_ERROR",
            Errors = errors
        };
    }

    public static PersianProblemDetails FromDomainException(
        DomainException exception,
        HttpStatusCode status,
        string? instance,
        string? traceId)
    {
        var (title, _) = TitleAndDetail(status);
        return new PersianProblemDetails
        {
            Type = TypeBase + ((int)status),
            Title = title,
            Status = (int)status,
            Detail = exception.Message,
            Instance = instance,
            TraceId = traceId,
            ErrorCode = exception.ErrorCode
        };
    }

    private static (string Title, string DefaultDetail) TitleAndDetail(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => (
            "درخواست نامعتبر است.",
            "درخواست ارسال‌شده قابل پردازش نیست."),
        HttpStatusCode.Unauthorized => (
            "احراز هویت لازم است.",
            "برای دسترسی به این منبع باید وارد حساب کاربری شوید."),
        HttpStatusCode.Forbidden => (
            "دسترسی غیرمجاز.",
            "شما اجازهٔ انجام این عملیات را ندارید."),
        HttpStatusCode.NotFound => (
            "منبع یافت نشد.",
            "منبع درخواستی در سامانه موجود نیست."),
        HttpStatusCode.Conflict => (
            "تعارض داده رخ داده است.",
            "به دلیل تغییر همزمان یا داده تکراری، عملیات قابل انجام نیست."),
        HttpStatusCode.UnprocessableEntity => (
            "قانون کسب‌وکار نقض شده است.",
            "درخواست از نظر ساختاری معتبر است اما با قوانین سامانه مغایرت دارد."),
        HttpStatusCode.TooManyRequests => (
            "درخواست‌های بیش از حد.",
            "تعداد درخواست‌های شما از حد مجاز عبور کرده است."),
        (HttpStatusCode)499 => (
            "درخواست لغو شد.",
            "درخواست پیش از تکمیل توسط کلاینت لغو شد."),
        HttpStatusCode.InternalServerError => (
            "خطای داخلی سرور.",
            "خطای غیرمنتظره‌ای رخ داده است. تیم فنی در جریان است."),
        _ => (
            "خطای پردازش درخواست.",
            "خطایی در پردازش درخواست رخ داده است.")
    };
}
