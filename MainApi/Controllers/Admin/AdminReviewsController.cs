using Application.Common.Interfaces.Admin.Product;
using Application.DTOs.Product;

namespace MainApi.Controllers.Admin;

[Route("api/admin/reviews")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IAdminReviewService _adminReviewService;

    public AdminReviewsController(IAdminReviewService adminReviewService)
    {
        _adminReviewService = adminReviewService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviewsByStatus([FromQuery] string status = "Pending", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminReviewService.GetReviewsByStatusAsync(status, page, pageSize);
        if (!result.Success) return StatusCode(500, new { message = result.Error });
        return Ok(result.Data);
    }

    [HttpPatch("{reviewId}/status")]
    public async Task<IActionResult> UpdateReviewStatus(int reviewId, [FromBody] UpdateReviewStatusDto dto)
    {
        var result = await _adminReviewService.UpdateReviewStatusAsync(reviewId, dto.Status);
        if (result.Success) return NoContent();
        return result.Error == "Review not found" ? NotFound() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var result = await _adminReviewService.DeleteReviewAsync(reviewId);
        if (result.Success) return NoContent();
        return result.Error == "Review not found" ? NotFound() : BadRequest(new { message = result.Error });
    }
}