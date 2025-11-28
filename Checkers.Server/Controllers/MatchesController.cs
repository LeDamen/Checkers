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
    }
}
