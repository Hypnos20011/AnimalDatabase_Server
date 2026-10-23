using AnimalDatabase.Api.RateLimiting;
using AnimalDatabase.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    [HttpGet("check")]
    [EnableRateLimiting(RateLimitPolicies.AdminCheck)]
    [RequireAdminKey]
    public IActionResult Check()
    {
        return NoContent();
    }
}
