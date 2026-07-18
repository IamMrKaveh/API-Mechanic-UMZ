using Application.Common.Interfaces;
using Presentation.Common.Interfaces;

namespace Presentation.Base.Endpoints.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private IHttpResultMapper? _httpResultMapper;
    private IMapper? _mapper;

    protected readonly IMediator Mediator;

    protected BaseApiController(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected BaseApiController(IMediator mediator, IMapper mapper)
    {
        Mediator = mediator;
        _mapper = mapper;
    }

    protected BaseApiController(IMediator mediator, IHttpResultMapper httpResultMapper)
    {
        Mediator = mediator;
        _httpResultMapper = httpResultMapper;
    }

    protected BaseApiController(IMediator mediator, IMapper mapper, IHttpResultMapper httpResultMapper)
    {
        Mediator = mediator;
        _mapper = mapper;
        _httpResultMapper = httpResultMapper;
    }

    protected IHttpResultMapper HttpResultMapper =>
        _httpResultMapper ??= HttpContext.RequestServices.GetRequiredService<IHttpResultMapper>();

    protected IMapper Mapper =>
        _mapper ??= HttpContext.RequestServices.GetRequiredService<IMapper>();

    protected IActionResult ToActionResult(ServiceResult result)
        => HttpResultMapper.Map(result);

    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
        => HttpResultMapper.Map(result);

    protected IActionResult ToActionResult(ServiceResult result, int statusCode)
    {
        var mapped = HttpResultMapper.Map(result);
        if (result.IsSuccess && mapped is ObjectResult obj)
            obj.StatusCode = statusCode;
        return mapped;
    }

    protected IActionResult ToActionResult<T>(ServiceResult<T> result, int statusCode)
    {
        var mapped = HttpResultMapper.Map(result);
        if (result.IsSuccess && mapped is ObjectResult obj)
            obj.StatusCode = statusCode;
        return mapped;
    }

    protected IActionResult ToCreatedActionResult<T>(ServiceResult<T> result, string? location = null)
        => HttpResultMapper.MapCreated(result, location);

    protected async Task<IActionResult> Send(IRequest<ServiceResult> request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(request, ct));

    protected async Task<IActionResult> Send<T>(IRequest<ServiceResult<T>> request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(request, ct));

    protected async Task<IActionResult> SendCreated<T>(
        IRequest<ServiceResult<T>> request,
        CancellationToken ct,
        string? location = null)
        => ToCreatedActionResult(await Mediator.Send(request, ct), location);
}