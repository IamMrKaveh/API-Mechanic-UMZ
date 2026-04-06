using Application.Common.Interfaces;
using Application.Common.Results;
using Asp.Versioning;
using SharedKernel.Contracts;
using SharedKernel.Models;

namespace Presentation.Base.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController(ISender mediator, ICurrentUserService currentUserService, IHttpResultMapper resultMapper) : ControllerBase
{
    protected readonly ISender Mediator = mediator;
    protected readonly ICurrentUserService CurrentUserService = currentUserService;
    protected readonly IHttpResultMapper ResultMapper = resultMapper;

    protected CurrentUser CurrentUser => CurrentUserService.CurrentUser;

    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        return ResultMapper.Map(result);
    }

    protected IActionResult ToActionResult(ServiceResult result)
    {
        return ResultMapper.Map(result);
    }
}