using Application.Attribute.Features.Commands.CreateAttributeType;
using Application.Attribute.Features.Commands.CreateAttributeValue;
using Application.Attribute.Features.Commands.DeleteAttributeType;
using Application.Attribute.Features.Commands.DeleteAttributeValue;
using Application.Attribute.Features.Commands.UpdateAttributeType;
using Application.Attribute.Features.Commands.UpdateAttributeValue;
using Application.Attribute.Features.Queries.GetAllAttributeTypes;
using Application.Attribute.Features.Queries.GetAttributeTypeById;
using Application.Attribute.Features.Shared;
using Presentation.Base.Controllers.v1;

namespace Presentation.Attribute.Controllers;

[ApiController]
[Route("api/admin/attributes")]
[Authorize(Roles = "Admin")]
public class AdminAttributesController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAllAttributeTypes(CancellationToken ct)
    {
        var command = new GetAllAttributeTypesQuery();
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAttributeType(
        int id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAttributeTypeByIdQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAttributeType(
        [FromBody] CreateAttributeTypeDto dto,
        CancellationToken ct)
    {
        var command = new CreateAttributeTypeCommand(dto.Name, dto.DisplayName, dto.SortOrder);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAttributeType(
        int id,
        [FromBody] UpdateAttributeTypeDto dto,
        CancellationToken ct)
    {
        var command = new UpdateAttributeTypeCommand(id, dto.Name, dto.DisplayName, dto.SortOrder, dto.IsActive);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttributeType(int id)
    {
        var result = await _mediator.Send(new DeleteAttributeTypeCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("{typeId}/values")]
    public async Task<IActionResult> CreateAttributeValue(
        int typeId,
        [FromBody] CreateAttributeValueDto dto)
    {
        var command = new CreateAttributeValueCommand(typeId, dto.Value, dto.DisplayValue, dto.HexCode, dto.SortOrder);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("values/{id}")]
    public async Task<IActionResult> UpdateAttributeValue(
        int id,
        [FromBody] UpdateAttributeValueDto dto)
    {
        var command = new UpdateAttributeValueCommand(id, dto.Value, dto.DisplayValue, dto.HexCode, dto.SortOrder, dto.IsActive);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("values/{id}")]
    public async Task<IActionResult> DeleteAttributeValue(int id)
    {
        var result = await _mediator.Send(new DeleteAttributeValueCommand(id));
        return ToActionResult(result);
    }
}