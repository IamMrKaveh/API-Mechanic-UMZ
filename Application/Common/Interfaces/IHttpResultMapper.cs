using Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Application.Common.Interfaces;

public interface IHttpResultMapper
{
    IActionResult Map(ServiceResult result);

    IActionResult Map<T>(ServiceResult<T> result);
}