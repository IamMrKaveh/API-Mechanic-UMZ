using Application.Common.Interfaces;
using Application.Common.Results;
using Asp.Versioning;
using MapsterMapper;
using Presentation.Common.Interfaces;
using SharedKernel.Models;

namespace Presentation.Base.Endpoints.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController(ISender mediator, IMapper? mapper = null) : ControllerBase
{
    protected readonly ISender Mediator = mediator;
    protected readonly IMapper? Mapper = mapper;

    private ICurrentUserService CurrentUserService =>
        HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    private IHttpResultMapper ResultMapper =>
        HttpContext.RequestServices.GetRequiredService<IHttpResultMapper>();

    protected CurrentUser CurrentUser => CurrentUserService.CurrentUser;
    protected string? GuestToken => CurrentUserService.GuestToken;
    protected bool IsAuthenticated => CurrentUserService.IsAuthenticated;

    protected IActionResult ToActionResult<T>(ServiceResult<T> result) =>
        ResultMapper.Map(result);

    protected IActionResult ToActionResult(ServiceResult result) =>
        ResultMapper.Map(result);
}