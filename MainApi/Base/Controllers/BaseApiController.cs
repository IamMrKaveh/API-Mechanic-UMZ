using Application.Common.Interfaces;
using Application.Common.Results;
using SharedKernel.Contracts;
using SharedKernel.Models;

namespace MainApi.Base.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController(ISender mediator) : ControllerBase
{
    protected readonly ISender Mediator = mediator;

    protected CurrentUser CurrentUser =>
        HttpContext.RequestServices.GetRequiredService<ICurrentUserService>().CurrentUser;

    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        var mapper = HttpContext.RequestServices.GetRequiredService<IHttpResultMapper>();
        return mapper.Map(result);
    }

    protected IActionResult ToActionResult(ServiceResult result)
    {
        var mapper = HttpContext.RequestServices.GetRequiredService<IHttpResultMapper>();
        return mapper.Map(result);
    }
}