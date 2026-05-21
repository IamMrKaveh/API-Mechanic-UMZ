using Application.Discount.Features.Commands.ApplyDiscount;
using Application.Discount.Features.Queries.ValidateDiscount;
using Application.Discount.Features.Shared;
using Presentation.Discount.Requests;

namespace Presentation.Discount.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/discounts")]
[Authorize]
public sealed class DiscountsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpPost("validation")]
    [ProducesResponseType(typeof(ApiResponse<DiscountValidationResult>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Validate([FromBody] ValidateDiscountRequest request)
    {
        var query = new ValidateDiscountQuery(
            request.Code,
            request.OrderAmount,
            CurrentUser.UserId,
            request.Currency);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("application")]
    [ProducesResponseType(typeof(ApiResponse<DiscountApplicationResult>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Apply([FromBody] ApplyDiscountRequest request)
    {
        var command = new ApplyDiscountCommand(
            request.Code,
            request.OrderAmount,
            CurrentUser.UserId,
            request.OrderId);

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}