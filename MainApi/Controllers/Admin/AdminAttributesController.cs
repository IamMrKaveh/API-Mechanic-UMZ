namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/attributes")]
[Authorize(Roles = "Admin")]
public class AdminAttributesController : ControllerBase
{
    private readonly IAdminAttributeService _adminAttributeService;
    private readonly ILogger<AdminAttributesController> _logger;

    public AdminAttributesController(
        IAdminAttributeService adminAttributeService,
        ILogger<AdminAttributesController> logger)
    {
        _adminAttributeService = adminAttributeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAttributeTypes()
    {
        var result = await _adminAttributeService.GetAllAttributeTypesAsync();
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAttributeType(int id)
    {
        var result = await _adminAttributeService.GetAttributeTypeByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAttributeType([FromBody] CreateAttributeTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminAttributeService.CreateAttributeTypeAsync(dto);
        if (!result.Success)
        {
            return Conflict(new { message = result.Error });
        }
        return CreatedAtAction(nameof(GetAttributeType), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAttributeType(int id, [FromBody] UpdateAttributeTypeDto dto)
    {
        var result = await _adminAttributeService.UpdateAttributeTypeAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Attribute type updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttributeType(int id)
    {
        var result = await _adminAttributeService.DeleteAttributeTypeAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Attribute type deleted successfully" });
    }

    [HttpPost("{typeId}/values")]
    public async Task<IActionResult> CreateAttributeValue(int typeId, [FromBody] CreateAttributeValueDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminAttributeService.CreateAttributeValueAsync(typeId, dto);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return CreatedAtAction(nameof(GetAttributeType), new { id = typeId }, result.Data);
    }

    [HttpPut("values/{id}")]
    public async Task<IActionResult> UpdateAttributeValue(int id, [FromBody] UpdateAttributeValueDto dto)
    {
        var result = await _adminAttributeService.UpdateAttributeValueAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Attribute value updated successfully" });
    }

    [HttpDelete("values/{id}")]
    public async Task<IActionResult> DeleteAttributeValue(int id)
    {
        var result = await _adminAttributeService.DeleteAttributeValueAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Attribute value deleted successfully" });
    }
}