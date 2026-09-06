using System.Net;
using FluentValidation.Results;
using Presentation.Common.ProblemDetails;
using SharedKernel.Exceptions;

namespace Tests.Presentation.Common.ProblemDetails;

public class PersianProblemDetailsFactoryTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400, "درخواست نامعتبر است.")]
    [InlineData(HttpStatusCode.Unauthorized, 401, "احراز هویت لازم است.")]
    [InlineData(HttpStatusCode.Forbidden, 403, "دسترسی غیرمجاز.")]
    [InlineData(HttpStatusCode.NotFound, 404, "منبع یافت نشد.")]
    [InlineData(HttpStatusCode.Conflict, 409, "تعارض داده رخ داده است.")]
    [InlineData(HttpStatusCode.UnprocessableEntity, 422, "قانون کسب‌وکار نقض شده است.")]
    [InlineData(HttpStatusCode.TooManyRequests, 429, "درخواست‌های بیش از حد.")]
    [InlineData(HttpStatusCode.InternalServerError, 500, "خطای داخلی سرور.")]
    [InlineData(HttpStatusCode.ServiceUnavailable, 503, "خطای پردازش درخواست.")]
    public void FromStatus_KnownCodes_ReturnPersianTitle(HttpStatusCode status, int code, string title)
    {
        var details = PersianProblemDetailsFactory.FromStatus(status, null, "/api/x", "trace-1", "E1");

        details.Type.ShouldBe($"https://ledka.ir/errors/{code}");
        details.Title.ShouldBe(title);
        details.Status.ShouldBe(code);
        details.Instance.ShouldBe("/api/x");
        details.TraceId.ShouldBe("trace-1");
        details.ErrorCode.ShouldBe("E1");
    }

    [Fact]
    public void FromStatus_ClientClosedRequest_Maps499()
    {
        var details = PersianProblemDetailsFactory.FromStatus((HttpStatusCode)499, null, null, null);

        details.Status.ShouldBe(499);
        details.Title.ShouldBe("درخواست لغو شد.");
        details.Type.ShouldBe("https://ledka.ir/errors/499");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromStatus_BlankDetail_UsesDefaultDetail(string? detail)
    {
        var details = PersianProblemDetailsFactory.FromStatus(HttpStatusCode.NotFound, detail, null, null);

        details.Detail.ShouldBe("منبع درخواستی در سامانه موجود نیست.");
    }

    [Fact]
    public void FromStatus_CustomDetail_IsPreserved()
    {
        var details = PersianProblemDetailsFactory.FromStatus(
            HttpStatusCode.Conflict, "رکورد تکراری است.", "/i", "t", "DUPLICATE_DATA");

        details.Detail.ShouldBe("رکورد تکراری است.");
        details.ErrorCode.ShouldBe("DUPLICATE_DATA");
    }

    [Fact]
    public void FromStatus_ErrorsDictionary_IsPreserved()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = ["bad"] };

        var details = PersianProblemDetailsFactory.FromStatus(HttpStatusCode.BadRequest, null, null, null, null, errors);

        details.Errors.ShouldBe(errors);
    }

    [Fact]
    public void FromValidation_GroupsFailuresByProperty()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "نام الزامی است."),
            new("Name", "نام کوتاه است."),
            new("", "خطای کلی.")
        };

        var details = PersianProblemDetailsFactory.FromValidation(failures, "/api/x", "trace-9");

        details.Status.ShouldBe(400);
        details.Type.ShouldBe("https://ledka.ir/errors/400");
        details.Title.ShouldBe("اطلاعات ورودی نامعتبر است.");
        details.ErrorCode.ShouldBe("VALIDATION_ERROR");
        details.Errors.ShouldNotBeNull();
        details.Errors!["Name"].ShouldBe(["نام الزامی است.", "نام کوتاه است."]);
        details.Errors["_"].ShouldBe(["خطای کلی."]);
    }

    [Fact]
    public void FromDomainException_UsesExceptionMessageAndCode()
    {
        var ex = new DomainException("WALLET_X", "موجودی کافی نیست.");

        var details = PersianProblemDetailsFactory.FromDomainException(ex, HttpStatusCode.BadRequest, "/i", "t");

        details.Status.ShouldBe(400);
        details.Title.ShouldBe("درخواست نامعتبر است.");
        details.Detail.ShouldBe("موجودی کافی نیست.");
        details.ErrorCode.ShouldBe("WALLET_X");
        details.Type.ShouldBe("https://ledka.ir/errors/400");
    }

    [Fact]
    public void PersianProblemDetails_HasExpectedDefaults()
    {
        var details = new PersianProblemDetails();

        details.Type.ShouldBe("about:blank");
        details.Title.ShouldBe(string.Empty);
        details.Detail.ShouldBeNull();
        details.Errors.ShouldBeNull();
        details.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void PersianProblemDetails_SerializesWithCamelCaseNames()
    {
        var details = new PersianProblemDetails
        {
            Title = "t",
            Status = 400,
            ErrorCode = "E",
            TraceId = "tr"
        };

        var json = JsonSerializer.Serialize(details);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("title").GetString().ShouldBe("t");
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("E");
        doc.RootElement.GetProperty("traceId").GetString().ShouldBe("tr");
    }
}
