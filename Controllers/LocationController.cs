using Microsoft.AspNetCore.Mvc;
using iot_server_cs;


namespace iot_server_cs.Controllers;

// add timestamp
public class LocationDto
{
    public required double latitude { get; set; }
    public required double longitude { get; set; }
    
}


[ApiController]
[Route("location")]
public class LocationController : ControllerBase
{

    private readonly AppDbContext _context;
    
    public LocationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult PostLocation([FromBody] LocationDto location)
    {
        _context.SetLocation(location);
        return Ok("location set");
    }
    
    [HttpGet]
    public IActionResult GetLocationPage()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot", "location","index.html");
        if (!System.IO.File.Exists(path))
        {
            return NotFound("File not found");
        }
        string content = System.IO.File.ReadAllText(path);
        return Content(content, "text/html");
    }
    
    [HttpPost]
    [Route("range/{value}")]
    public IActionResult PostLocationRange(double value)
    {
        _context.SetLocationRange(value);
        return Ok("location range set");
    }
    
    [HttpPost]
    [Route("center")]
    public IActionResult PostCenterLocation([FromBody] LocationDto location)
    {
        _context.SetLocationRangeCenter(location.latitude, location.longitude);
        return Ok("center location set");
    }
    
}