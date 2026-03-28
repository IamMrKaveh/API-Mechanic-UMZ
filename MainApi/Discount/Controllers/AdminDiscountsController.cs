using MainApi.Discount.Requests;

namespace MainApi.Discount.Controllers;

[ApiController]
[Route("api/admin/discounts")]
[Authorize(Roles = "Admin")]
public class AdminDiscountsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<DiscountCodeDto>>> GetAll(
        [FromQuery] bool includeExpired = false,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetDiscountsQuery(includeExpired, includeDeleted, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetDiscountByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpGet("{id}/usage-report")]
    public async Task<IActionResult> GetUsageReport(int id)
    {
        var result = await _mediator.Send(new GetDiscountUsageReportQuery(id));
        return ToActionResult(result);
    }

    [HttpGet("info/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDiscountInfo(string code)
    {
        var result = await _mediator.Send(new GetDiscountInfoQuery(code));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<DiscountCodeDto>>> Create(CreateDiscountCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResult>> Update(UpdateDiscountCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResult>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteDiscountCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("{id}/cancel-usage")]
    public async Task<IActionResult> CancelDiscountUsage(int id, [FromBody] CancelDiscountUsageRequest request)
    {
        var command = new CancelDiscountUsageCommand(request.OrderId, id);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}