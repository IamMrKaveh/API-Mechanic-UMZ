using Application.Discount.Features.Commands.CancelDiscountUsage;
using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Commands.DeleteDiscount;
using Application.Discount.Features.Commands.UpdateDiscount;
using Application.Discount.Features.Queries.GetDiscountById;
using Application.Discount.Features.Queries.GetDiscountInfo;
using Application.Discount.Features.Queries.GetDiscounts;
using Application.Discount.Features.Queries.GetDiscountUsageReport;
using MapsterMapper;
using Presentation.Discount.Requests;

namespace Presentation.Discount.Endpoints;

[ApiController]
[Route("api/admin/discounts")]
[Authorize(Roles = "Admin")]
public sealed class AdminDiscountsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeExpired = false,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetDiscountsQuery(includeExpired, includeDeleted, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetDiscountByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/usage-report")]
    public async Task<IActionResult> GetUsageReport(Guid id)
    {
        var result = await Mediator.Send(new GetDiscountUsageReportQuery(id));
        return ToActionResult(result);
    }

    [HttpGet("info/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDiscountInfo(string code)
    {
        var result = await Mediator.Send(new GetDiscountInfoQuery(code));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDiscountRequest request)
    {
        var command = Mapper.Map<CreateDiscountCommand>(request);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDiscountRequest request)
    {
        var command = Mapper.Map<UpdateDiscountCommand>(request) with { Id = id };
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteDiscountCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel-usage")]
    public async Task<IActionResult> CancelDiscountUsage(Guid id, [FromBody] CancelDiscountUsageRequest request)
    {
        var command = new CancelDiscountUsageCommand(id, request.OrderId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}