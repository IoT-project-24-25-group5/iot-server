using Microsoft.AspNetCore.Mvc;

namespace iot_server_cs.Controllers;

[ApiController]
[Route("sensors")]
public class SensorsController : ControllerBase
{

    private readonly AppDbContext _context;
    
    public SensorsController(AppDbContext context)
    {
        _context = context;
    }
    
    
    [HttpPost("{name}/{value}")]
    public IActionResult PostSensorData(string name, string value)
    {
        _context.AddSensorData(name, value);
        _context.SaveChanges();
        Console.WriteLine($"name: {name}, value: {value}");
        foreach (var VARIABLE in _context.GetSensors())
        {
            Console.WriteLine($"id: {VARIABLE.Id}, value: {VARIABLE.Value}");
        }
        
        return Ok("Sensor data saved");
    }
    
    
}