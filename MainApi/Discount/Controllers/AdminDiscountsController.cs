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
    public async Task<ActionResult<PaginatedResult<DiscountCodeDto>>> GetAll([FromQuery] bool includeExpired = false, [FromQuery] bool includeDeleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetDiscountsQuery(includeExpired, includeDeleted, page, pageSize));
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
}