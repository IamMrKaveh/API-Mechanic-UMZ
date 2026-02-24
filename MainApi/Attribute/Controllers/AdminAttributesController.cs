namespace MainApi.Attribute.Controllers;

[ApiController]
[Route("api/admin/attributes")]
[Authorize(Roles = "Admin")]
public class AdminAttributesController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminAttributesController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAttributeTypes()
    {
        var result = await _mediator.Send(new GetAllAttributeTypesQuery());
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAttributeType(int id)
    {
        var result = await _mediator.Send(new GetAttributeTypeByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAttributeType([FromBody] CreateAttributeTypeDto dto)
    {
        var command = new CreateAttributeTypeCommand(dto.Name, dto.DisplayName, dto.SortOrder);
        var result = await _mediator.Send(command);

        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetAttributeType), new { id = result.Data!.Id }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAttributeType(int id, [FromBody] UpdateAttributeTypeDto dto)
    {
        var command = new UpdateAttributeTypeCommand(id, dto.Name, dto.DisplayName, dto.SortOrder, dto.IsActive);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttributeType(int id)
    {
        var result = await _mediator.Send(new DeleteAttributeTypeCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("{typeId}/values")]
    public async Task<IActionResult> CreateAttributeValue(int typeId, [FromBody] CreateAttributeValueDto dto)
    {
        var command = new CreateAttributeValueCommand(typeId, dto.Value, dto.DisplayValue, dto.HexCode, dto.SortOrder);
        var result = await _mediator.Send(command);

        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetAttributeType), new { id = typeId }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPut("values/{id}")]
    public async Task<IActionResult> UpdateAttributeValue(int id, [FromBody] UpdateAttributeValueDto dto)
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