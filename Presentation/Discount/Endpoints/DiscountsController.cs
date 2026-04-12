using Application.Discount.Features.Commands.ApplyDiscount;
using Application.Discount.Features.Queries.ValidateDiscount;
using Presentation.Discount.Requests;

namespace Presentation.Discount.Endpoints;

[ApiController]
[Route("api/discounts")]
[Authorize]
public sealed class DiscountsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpPost("validate")]
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

    [HttpPost("apply")]
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