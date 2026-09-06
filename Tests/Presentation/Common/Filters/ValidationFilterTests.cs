using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Presentation.Base.Responses;
using Presentation.Common.Filters;

namespace Tests.Presentation.Common.Filters;

public class ValidationFilterTests
{
    private static ActionExecutingContext BuildContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    [Fact]
    public void OnActionExecuting_WhenModelStateIsValid_DoesNotSetResult()
    {
        var context = BuildContext();
        var filter = new ValidationFilter();

        filter.OnActionExecuting(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public void OnActionExecuting_WhenModelStateIsInvalid_ReturnsBadRequestWithErrors()
    {
        var context = BuildContext();
        context.ModelState.AddModelError("Name", "نام الزامی است.");
        context.ModelState.AddModelError("Email", "ایمیل نامعتبر است.");
        var filter = new ValidationFilter();

        filter.OnActionExecuting(context);

        var badRequest = context.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(400);
        var body = badRequest.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeFalse();
        body.Message.ShouldBe("اطلاعات ورودی نامعتبر است.");
        body.Errors.ShouldNotBeNull();
        body.Errors!["Name"].ShouldBe(["نام الزامی است."]);
        body.Errors["Email"].ShouldBe(["ایمیل نامعتبر است."]);
    }

    [Fact]
    public void OnActionExecuting_WhenMultipleErrorsForSameKey_ReturnsAllMessages()
    {
        var context = BuildContext();
        context.ModelState.AddModelError("Name", "first");
        context.ModelState.AddModelError("Name", "second");
        var filter = new ValidationFilter();

        filter.OnActionExecuting(context);

        var body = context.Result.ShouldBeOfType<BadRequestObjectResult>()
            .Value.ShouldBeOfType<ApiResponse>();
        body.Errors!["Name"].ShouldBe(["first", "second"]);
    }

    [Fact]
    public void OnActionExecuted_DoesNothing()
    {
        var filter = new ValidationFilter();
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        var executed = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object());

        Should.NotThrow(() => filter.OnActionExecuted(executed));
    }
}
