namespace MainApi.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
            context.Result = BuildValidationErrorResult(context);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    { }

    private static BadRequestObjectResult BuildValidationErrorResult(ActionExecutingContext context)
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new
        {
            message = "Validation failed",
            errors
        });
    }
}