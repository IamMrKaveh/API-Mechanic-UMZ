using Application.Common.Interfaces;
using Application.DTOs;
using MainApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IReviewService _reviewService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserService userService, IReviewService reviewService, ICurrentUserService currentUserService, ILogger<ProfileController> logger)
    {
        _userService = userService;
        _reviewService = reviewService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _userService.GetUserProfileAsync(userId.Value);
        if (!result.Success) return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateRequest)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _userService.UpdateProfileAsync(userId.Value, updateRequest);
        if (result.Success) return NoContent();

        return result.Error == "NotFound" ? NotFound() : StatusCode(500, "An error occurred");
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _userService.DeleteAccountAsync(userId.Value);
        if (result.Success) return Ok(new { message = "Account successfully deleted." });

        return result.Error == "NotFound" ? NotFound() : StatusCode(500, "An error occurred");
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetMyReviews()
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _reviewService.GetUserReviewsAsync(userId.Value);
        if (!result.Success) return StatusCode(500, new { message = result.Error });

        return Ok(result.Data);
    }
}