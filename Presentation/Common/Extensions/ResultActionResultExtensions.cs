using Presentation.Common.Interfaces;

namespace Presentation.Common.Extensions;

public static class ResultActionResultExtensions
{
    public static IActionResult ToActionResult(this ServiceResult result, IHttpResultMapper mapper)
        => mapper.Map(result);

    public static IActionResult ToActionResult<T>(this ServiceResult<T> result, IHttpResultMapper mapper)
        => mapper.Map(result);

    public static IActionResult ToCreatedActionResult<T>(this ServiceResult<T> result, IHttpResultMapper mapper, string? location = null)
        => mapper.MapCreated(result, location);
}