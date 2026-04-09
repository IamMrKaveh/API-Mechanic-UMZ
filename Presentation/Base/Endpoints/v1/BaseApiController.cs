using Application.Common.Interfaces;
using Application.Common.Results;
using Asp.Versioning;
using SharedKernel.Contracts;
using SharedKernel.Models;

namespace Presentation.Base.Endpoints.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController(ISender mediator) : ControllerBase
{
    protected readonly ISender Mediator = mediator;

    private ICurrentUserService CurrentUserService =>
        HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    private IHttpResultMapper ResultMapper =>
        HttpContext.RequestServices.GetRequiredService<IHttpResultMapper>();

    protected CurrentUser CurrentUser => CurrentUserService.CurrentUser;
    protected string? GuestId => CurrentUserService.GuestId;
    protected bool IsAuthenticated => CurrentUserService.IsAuthenticated;

    protected IActionResult ToActionResult<T>(ServiceResult<T> result) =>
        ResultMapper.Map(result);

    protected IActionResult ToActionResult(ServiceResult result) =>
        ResultMapper.Map(result);
}