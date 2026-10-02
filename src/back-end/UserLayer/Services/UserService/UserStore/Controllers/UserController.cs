using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserStore.Business.Usr;
using UserStore.Models.Requests;
using UserStore.Models.Responses;

namespace UserStore.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUsrService _userService;
    private readonly ILogger<UserController> _logger;
    private const string domain = "[CONTROLLER][USER]";

    public UserController(IUsrService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    [Route("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterUserRequest request)
    {
        _logger.LogInformation(
            "{Domain}[REGISTER] Registration attempt for username: {Username}, email: {Email}\n",
            domain, request.Username, request.Email);

        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest("All fields are required.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        var result = await _userService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetProfile), new { id = result.UserId }, result);
    }

    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation(
            "{Domain}[LOGIN] Login attempt for email: {Email}\n",
            domain, request.Email);

        var result = await _userService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost]
    [Route("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("{Domain}[LOGOUT] Logout attempt for user: {UserId}\n", domain, GetUserId());

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        await _userService.LogoutAsync(token);
        return NoContent();
    }

    [HttpGet]
    [Route("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        _logger.LogInformation("{Domain}[PROFILE] Profile request for user: {UserId}\n", domain, GetUserId());

        var userId = GetUserId();
        var profile = await _userService.GetProfileAsync(userId);
        return Ok(profile);
    }

    [HttpPut]
    [Route("update")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        _logger.LogInformation("{Domain}[UPDATE_PROFILE] Update profile request for user: {UserId}\n", domain, GetUserId());

        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Username and Email are required.");
        }

        if (!string.IsNullOrEmpty(request.Password) &&
            request.Password != request.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        var userId = GetUserId();
        await _userService.UpdateProfileAsync(userId, request);
        return NoContent();
    }

    [HttpPost]
    [Route("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        _logger.LogInformation(
            "{Domain}[RESET_PASSWORD] Password reset attempt for email: {Email}\n",
            domain, request.Email);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _userService.ResetPasswordAsync(request);
        return NoContent();
    }

    [HttpGet]
    [Route("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAllUsers()
    {
        _logger.LogInformation("{Domain}[GET_ALL_USERS] Admin fetching all users\n", domain);
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        _logger.LogInformation("{Domain}[DELETE_USER] Admin deleting user {UserId}\n", domain, id);
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(ClaimTypes.Name);

        return int.TryParse(userId, out var id) ? id : throw new UnauthorizedAccessException("Invalid user ID.");
    }
}
