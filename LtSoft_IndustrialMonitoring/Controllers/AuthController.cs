using LtSoft_IndustrialMonitoring.Interfaces;

using Microsoft.AspNetCore.Mvc;

using static LtSoft_IndustrialMonitoring.Models.LoginModels;

namespace LtSoft_IndustrialMonitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest credentials)
        {
            if (string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password))
            {
                return BadRequest("Username and password are required");
            }

            LoginResponse result = await _authService.AuthenticateAsync(credentials);
            if (result == null)
            {
                return Unauthorized("Invalid credentials");
            }

            return Ok(result);
        }
    }
}
