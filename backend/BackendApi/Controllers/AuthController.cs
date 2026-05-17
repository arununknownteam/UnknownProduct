using BackendApi.DTOs;
using BackendApi.Services; 
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly LoginService _loginService;

        public AuthController(LoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            try
            {
                _loginService.Register(request);

                return Ok(new
                {
                    message = "User registered successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            try
            {
                var token = _loginService.Login(request);

                return Ok(new
                {
                    token = token
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new
                {
                    error = ex.Message
                });
            }
        }
    }
}