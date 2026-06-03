using Application.Inventory.Features.Commands.CreateWarehouse;
using Application.Inventory.Features.Commands.DeleteWarehouse;
using Application.Inventory.Features.Commands.SetDefaultWarehouse;
using Application.Inventory.Features.Commands.ToggleWarehouseActive;
using Application.Inventory.Features.Commands.UpdateWarehouse;
using Application.Inventory.Features.Queries.GetAllWarehouses;
using Application.Inventory.Features.Queries.GetWarehouseById;
using Application.Inventory.Features.Shared;
using Presentation.Inventory.Requests;

namespace Presentation.Inventory.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/warehouses")]
[Authorize(Roles = "Admin")]
public sealed class AdminWarehouseController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WarehouseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var query = new GetAllWarehousesQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetWarehouseByIdQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        var command = new CreateWarehouseCommand(
            request.Code,
            request.Name,
            request.City,
            request.Address,
            request.Phone,
            request.Priority,
            request.IsDefault);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken ct)
    {
        var command = new UpdateWarehouseCommand(
            id,
            request.Name,
            request.City,
            request.Address,
            request.Phone,
            request.Priority);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeleteWarehouseCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/set-default-warehouse")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultWarehouse(Guid id, CancellationToken ct)
    {
        var command = new SetDefaultWarehouseCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleWarehouseStatusRequest request, CancellationToken ct)
    {
        var command = new ToggleWarehouseActiveCommand(id, request.IsActive);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}