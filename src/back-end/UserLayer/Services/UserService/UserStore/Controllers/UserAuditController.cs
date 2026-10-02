using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserData.Repositories.Audit;
using UserStore.Models.Responses;

namespace UserStore.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")] // Only Admins can access
public class UserAuditController : ControllerBase
{
    private readonly IUserAuditRepository _auditRepository;
    private readonly ILogger<UserAuditController> _logger;
    private const string domain = "[CONTROLLER][USER_AUDIT]";

    public UserAuditController(IUserAuditRepository auditRepository, ILogger<UserAuditController> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    [HttpGet]
    [Route("user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<UserAuditResponse>>> GetUserAudits(int userId)
    {
        _logger.LogInformation("{Domain} Fetching audits for user {UserId}", domain, userId);

        var audits = await _auditRepository.GetUserAuditsAsync(userId);

        var response = audits.Select(a => new UserAuditResponse
        {
            AuditId = a.AuditId,
            UserId = a.UserId,
            Action = a.Action,
            Details = a.Details,
            Timestamp = a.Timestamp
        });

        return Ok(response);
    }
}
