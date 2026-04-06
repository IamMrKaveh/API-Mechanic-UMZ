using Application.Discount.Features.Commands.ApplyDiscount;
using Application.Discount.Features.Queries.ValidateDiscount;
using Presentation.Base.Controllers.v1;
using Presentation.Discount.Requests;

namespace Presentation.Discount.Controllers;

[ApiController]
[Route("api/discounts")]
[Authorize]
public class DiscountsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateDiscountRequest request)
    {
        var query = new ValidateDiscountQuery(request.Code, request.OrderTotal, CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyDiscountRequest request)
    {
        var command = new ApplyDiscountCommand(request.Code, request.OrderTotal, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}