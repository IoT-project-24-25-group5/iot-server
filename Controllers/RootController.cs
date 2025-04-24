// using Microsoft.AspNetCore.Mvc;
//
// namespace iot_server_cs.Controllers;
//
// [ApiController]
// [Route("/")]
// public class RootController : ControllerBase
// {
//     [HttpGet]
//     public IActionResult Index()
//     {
//         var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
//         if (!System.IO.File.Exists(filePath))
//         {
//             return NotFound("File not found");
//         }
//         var content = System.IO.File.ReadAllText(filePath);
//         return Content(content, "text/html");
//     }
// }