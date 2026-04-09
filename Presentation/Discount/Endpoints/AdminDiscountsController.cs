using Application.Discount.Features.Commands.CancelDiscountUsage;
using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Commands.DeleteDiscount;
using Application.Discount.Features.Commands.UpdateDiscount;
using Application.Discount.Features.Queries.GetDiscountById;
using Application.Discount.Features.Queries.GetDiscountInfo;
using Application.Discount.Features.Queries.GetDiscounts;
using Application.Discount.Features.Queries.GetDiscountUsageReport;
using Domain.Discount.Enums;
using Presentation.Discount.Requests;

namespace Presentation.Discount.Endpoints;

[ApiController]
[Route("api/admin/discounts")]
[Authorize(Roles = "Admin")]
public class AdminDiscountsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeExpired = false,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetDiscountsQuery(includeExpired, includeDeleted, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetDiscountByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpGet("{id}/usage-report")]
    public async Task<IActionResult> GetUsageReport(Guid id)
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
    public async Task<IActionResult> Create([FromBody] CreateDiscountRequest request)
    {
        if (!Enum.TryParse<DiscountType>(request.DiscountType, true, out var discountType))
            return BadRequest("Invalid discount type.");

        var command = new CreateDiscountCommand(
            request.Code,
            discountType,
            request.DiscountValue,
            request.MaximumDiscountAmount,
            request.UsageLimit,
            request.StartsAt,
            request.ExpiresAt);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDiscountRequest request)
    {
        if (!Enum.TryParse<DiscountType>(request.DiscountType, true, out var discountType))
            return BadRequest("Invalid discount type.");

        var command = new UpdateDiscountCommand(
            id,
            discountType,
            request.DiscountValue,
            request.MaximumDiscountAmount,
            request.UsageLimit,
            request.StartsAt,
            request.ExpiresAt,
            request.IsActive);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteDiscountCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("{id}/cancel-usage")]
    public async Task<IActionResult> CancelDiscountUsage(Guid id, [FromBody] CancelDiscountUsageRequest request)
    {
        var command = new CancelDiscountUsageCommand(request.OrderId, id);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}