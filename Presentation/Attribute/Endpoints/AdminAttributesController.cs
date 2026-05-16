using Application.Attribute.Features.Commands.CreateAttributeType;
using Application.Attribute.Features.Commands.CreateAttributeValue;
using Application.Attribute.Features.Commands.DeleteAttributeType;
using Application.Attribute.Features.Commands.DeleteAttributeValue;
using Application.Attribute.Features.Commands.UpdateAttributeType;
using Application.Attribute.Features.Commands.UpdateAttributeValue;
using Application.Attribute.Features.Queries.GetAllAttributeTypes;
using Application.Attribute.Features.Queries.GetAttributeTypeById;
using Application.Attribute.Features.Shared;
using Presentation.Attribute.Requests;

namespace Presentation.Attribute.Endpoints;

[ApiController]
[Route("api/admin/attributes")]
[Authorize(Roles = "Admin")]
public class AdminAttributesController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AttributeTypeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAttributeTypes(CancellationToken ct)
    {
        var query = new GetAllAttributeTypesQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AttributeTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttributeType(Guid id, CancellationToken ct)
    {
        var query = new GetAttributeTypeByIdQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AttributeTypeDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAttributeType(
        [FromBody] CreateAttributeTypeRequest request,
        CancellationToken ct)
    {
        var command = new CreateAttributeTypeCommand(
            request.Name,
            request.DisplayName,
            request.SortOrder);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{typeId:guid}/values")]
    [ProducesResponseType(typeof(ApiResponse<AttributeValueDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAttributeValue(
    Guid typeId,
    [FromBody] CreateAttributeValueRequest request,
    CancellationToken ct)
    {
        var command = new CreateAttributeValueCommand(
            typeId,
            request.Value,
            request.DisplayValue,
            request.HexCode,
            request.SortOrder);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAttributeType(
        Guid id,
        [FromBody] UpdateAttributeTypeRequest request,
        CancellationToken ct)
    {
        var command = new UpdateAttributeTypeCommand(
            id,
            request.Name,
            request.DisplayName,
            request.SortOrder,
            request.IsActive);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("values/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAttributeValue(
    Guid id,
    [FromBody] UpdateAttributeValueRequest request,
    CancellationToken ct)
    {
        var command = new UpdateAttributeValueCommand(
            id,
            request.Value,
            request.DisplayValue,
            request.HexCode,
            request.SortOrder,
            request.IsActive);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttributeType(Guid id, CancellationToken ct)
    {
        var command = new DeleteAttributeTypeCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("values/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttributeValue(Guid id, CancellationToken ct)
    {
        var command = new DeleteAttributeValueCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}