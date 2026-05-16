using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.DeleteShipping;
using Application.Shipping.Features.Commands.RestoreShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Application.Shipping.Features.Queries.GetShipping;
using Application.Shipping.Features.Queries.GetShippings;
using Application.Shipping.Features.Shared;
using Presentation.Shipping.Requests;

namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public sealed class AdminShippingsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShippingListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShippings(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = new GetShippingsQuery(includeDeleted);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShippingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShippingById(Guid id, CancellationToken ct)
    {
        var query = new GetShippingQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ShippingDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateShipping(
        [FromBody] CreateShippingRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<CreateShippingCommand>(request);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShippingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShipping(
        Guid id,
        [FromBody] UpdateShippingRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<UpdateShippingCommand>(request) with { Id = id };
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShipping(Guid id, CancellationToken ct)
    {
        var command = new DeleteShippingCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreShipping(Guid id, CancellationToken ct)
    {
        var command = new RestoreShippingCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}