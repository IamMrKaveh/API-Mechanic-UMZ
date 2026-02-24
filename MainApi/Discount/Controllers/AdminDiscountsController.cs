namespace MainApi.Discount.Controllers;

[ApiController]
[Route("api/admin/discounts")]
[Authorize(Roles = "Admin")]
public class AdminDiscountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminDiscountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<DiscountCodeDto>>> GetAll(
        [FromQuery] bool includeExpired = false,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetDiscountsQuery(includeExpired, includeDeleted, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetDiscountByIdQuery(id));
        if (result.IsFailed) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{id}/usage-report")]
    public async Task<IActionResult> GetUsageReport(int id)
    {
        var result = await _mediator.Send(new GetDiscountUsageReportQuery(id));
        if (result.IsFailed) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("info/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDiscountInfo(string code)
    {
        var result = await _mediator.Send(new GetDiscountInfoQuery(code));
        if (result.IsFailed) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<DiscountCodeDto>>> Create(CreateDiscountCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailed)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResult>> Update(int id, UpdateDiscountCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        if (result.IsFailed)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResult>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteDiscountCommand(id));
        if (result.IsFailed)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id}/cancel-usage")]
    public async Task<IActionResult> CancelDiscountUsage(int id, [FromBody] CancelDiscountUsageRequest request)
    {
        var command = new CancelDiscountUsageCommand(request.OrderId, id);
        var result = await _mediator.Send(command);
        if (result.IsFailed) return BadRequest(result);
        return Ok(result);
    }
}

public record CancelDiscountUsageRequest(int OrderId);