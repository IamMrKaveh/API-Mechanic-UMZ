using Application.Common.Results;

namespace Presentation.Common.Interfaces;

public interface IHttpResultMapper
{
    IActionResult Map(ServiceResult result);

    IActionResult Map<T>(ServiceResult<T> result);
}