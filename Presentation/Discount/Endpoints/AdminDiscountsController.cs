using Application.Discount.Features.Commands.CancelDiscountUsage;
using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Commands.DeleteDiscount;
using Application.Discount.Features.Commands.UpdateDiscount;
using Application.Discount.Features.Queries.GetDiscountById;
using Application.Discount.Features.Queries.GetDiscountInfo;
using Application.Discount.Features.Queries.GetDiscounts;
using Application.Discount.Features.Queries.GetDiscountUsageReport;
using Application.Discount.Features.Shared;
using Presentation.Discount.Requests;

namespace Presentation.Discount.Endpoints;

[ApiController]
[Route("api/admin/discounts")]
[Authorize(Roles = "Admin")]
public sealed class AdminDiscountsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<DiscountCodeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeExpired = false,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetDiscountsQuery(includeExpired, includeDeleted, page, pageSize);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DiscountCodeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetDiscountByIdQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/usage-report")]
    [ProducesResponseType(typeof(ApiResponse<DiscountUsageReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsageReport(Guid id)
    {
        var query = new GetDiscountUsageReportQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("info/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DiscountInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscountInfo(string code)
    {
        var query = new GetDiscountInfoQuery(code);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DiscountDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDiscountRequest request)
    {
        var command = Mapper.Map<CreateDiscountCommand>(request);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DiscountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DiscountDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DiscountDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDiscountRequest request)
    {
        var command = Mapper.Map<UpdateDiscountCommand>(request) with { Id = id };
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteDiscountCommand(id);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel-usage")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CancelDiscountUsage(Guid id, [FromBody] CancelDiscountUsageRequest request)
    {
        var command = new CancelDiscountUsageCommand(id, request.OrderId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}