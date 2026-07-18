namespace Presentation.Common.Interfaces;

public interface IHttpResultMapper
{
    IActionResult Map(ServiceResult result);

    IActionResult Map<T>(ServiceResult<T> result);

    IActionResult MapCreated<T>(ServiceResult<T> result, string? location = null);
}