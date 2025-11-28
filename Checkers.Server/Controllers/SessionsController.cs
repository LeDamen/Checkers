using Microsoft.AspNetCore.Mvc;
using Checkers.Server.Data;
using Checkers.Server.Models;
using System.Linq;

namespace Checkers.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SessionsController(AppDbContext db)
        {
            _db = db;
        }

        // POST api/sessions/register
        [HttpPost("register")]
        public ActionResult<int> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Name))
                return BadRequest("Name is required");

            var user = _db.Users.FirstOrDefault(u => u.Username == req.Name);
            if (user == null)
            {
                user = new User { Username = req.Name };
                _db.Users.Add(user);
                _db.SaveChanges();
            }

            return Ok(user.Id);
        }

        // POST api/sessions/restore
        [HttpPost("restore")]
        public ActionResult Restore([FromBody] RestoreRequest req)
        {
            if (req == null || req.UserId <= 0)
                return BadRequest("UserId required");

            var user = _db.Users.Find(req.UserId);
            if (user == null)
                return NotFound();

            return Ok(new { user.Id, user.Username });
        }
    }

    public class RegisterRequest { public string? Name { get; set; } }
    public class RestoreRequest { public int UserId { get; set; } }
}
