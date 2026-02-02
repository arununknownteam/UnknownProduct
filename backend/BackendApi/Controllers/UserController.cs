using BackendApi.Entities;
using BackendApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _repo = new();

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_repo.GetAll());
        }

        [HttpPost]
        public IActionResult Post(User user)
        {
            _repo.Add(user);
            return Ok();
        }
    }
}
