// HELLOCONTROLLER: handles HTTP requests for the URL /api/hello.
//
// Interview line: "A controller maps HTTP verbs and URLs to methods. I return the right
// status codes and validate input before doing any work."

using Microsoft.AspNetCore.Mvc;

namespace HelloApi.Controllers;

[ApiController]            // turns on API behaviors, such as automatic 400 for badly shaped JSON
[Route("api/[controller]")] // [controller] becomes "hello", so the base URL is /api/hello
public class HelloController : ControllerBase
{
    private readonly IConfiguration _config;

    // DEPENDENCY INJECTION: we do not create IConfiguration ourselves. ASP.NET Core hands
    // it to us through the constructor. This keeps code testable and loosely coupled.
    public HelloController(IConfiguration config) => _config = config;

    // GET /api/hello
    // GET means "read something". It should not change data.
    // The message comes from configuration: appsettings.json by default, or the Key Vault
    // secret "HelloMessage" once Stage 4 is on. The code does not know or care which.
    [HttpGet]
    public IActionResult Get()
    {
        var message = _config["HelloMessage"] ?? "Hello, World!"; // "??" means "if null use this"
        return Ok(new { message, time = DateTime.UtcNow });       // Ok(...) = HTTP 200
    }

    // GET /api/hello/Kaushik
    // {name} in the route is a parameter taken from the URL.
    [HttpGet("{name}")]
    public IActionResult GetByName(string name)
    {
        // VALIDATION: never trust input. Reject bad input early with 400 (the caller's mistake).
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            return BadRequest(new { error = "Name must be 1 to 50 characters." });

        return Ok(new { message = $"Hello, {name}!" });
    }

    // POST /api/hello   body: { "name": "Kaushik" }
    // POST means "create something". [FromBody] reads the JSON the caller sent.
    [HttpPost]
    public IActionResult Post([FromBody] GreetingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        // 201 Created is the right status for a POST that creates something.
        // (A real API would also return a Location header pointing to the new item.)
        return StatusCode(StatusCodes.Status201Created,
            new { message = $"Hello, {request.Name}! (created)" });
    }
}

// A "record" is a short way to declare a simple data class (an immutable DTO).
public record GreetingRequest(string Name);
