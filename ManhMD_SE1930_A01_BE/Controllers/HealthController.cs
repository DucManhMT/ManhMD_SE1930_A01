using Microsoft.AspNetCore.Mvc;

namespace ManhMD_SE1930_A01_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "FUNews Management API",
            Timestamp = DateTime.UtcNow
        });
    }
}
