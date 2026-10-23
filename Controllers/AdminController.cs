using AnimalDatabase.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    [HttpGet("check")]
    [RequireAdminKey]
    public IActionResult Check()
    {
        return NoContent();
    }
}
