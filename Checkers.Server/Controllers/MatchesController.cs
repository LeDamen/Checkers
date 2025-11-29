using Microsoft.AspNetCore.Mvc;
using Checkers.Server.Data;
using System.Linq;

namespace Checkers.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MatchesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public ActionResult GetAll()
        {
            var list = _db.Matches.OrderByDescending(m => m.DatePlayed).ToList();
            return Ok(list);
        }

        // POST api/matches/test-create
        // Create a test Match record. If player names provided, ensure Users exist and link them.
        [HttpPost("test-create")]
        public ActionResult CreateTest([FromBody] TestMatchRequest req)
        {
            if (req == null) req = new TestMatchRequest();

            int? p1 = null, p2 = null;
            if (!string.IsNullOrWhiteSpace(req.Player1))
            {
                var u1 = _db.Users.FirstOrDefault(u => u.Username == req.Player1);
                if (u1 == null) { u1 = new Models.User { Username = req.Player1 }; _db.Users.Add(u1); _db.SaveChanges(); }
                p1 = u1.Id;
            }

            if (!string.IsNullOrWhiteSpace(req.Player2))
            {
                var u2 = _db.Users.FirstOrDefault(u => u.Username == req.Player2);
                if (u2 == null) { u2 = new Models.User { Username = req.Player2 }; _db.Users.Add(u2); _db.SaveChanges(); }
                p2 = u2.Id;
            }

            var match = new Models.Match
            {
                Player1Id = p1,
                Player2Id = p2,
                WinnerId = null,
                MovesJson = req.MovesJson ?? "[]",
                DatePlayed = DateTime.UtcNow
            };

            _db.Matches.Add(match);
            _db.SaveChanges();

            return Ok(new { match.Id });
        }
    }

    public class TestMatchRequest
    {
        public string? Player1 { get; set; }
        public string? Player2 { get; set; }
        public string? MovesJson { get; set; }
    }
}
