using BackendApi.NHibernate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ISession = NHibernate.ISession;
namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ISession _session;

        public UserController(ISession session)
        {
            _session = session;
        }

        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            var user = _session.Query<BackendApi.Entities.User>()
                .FirstOrDefault(u => u.Email == email);
            
            if (user == null)
                return NotFound(new { error = "User not found" });

            AppLogger.Info($"{user.UserName} Login Suggessfully");
            
            return Ok(new
            {
                userName = user.UserName,
                email = user.Email,
                bio = user.Bio,
                profileImageUrl = user.ProfileImageUrl,
                createdAt = user.CreatedAt
            });
        }
    }
}