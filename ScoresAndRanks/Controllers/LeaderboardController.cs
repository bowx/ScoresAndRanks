using Microsoft.AspNetCore.Mvc;
using ScoresAndRanks.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScoresAndRanks.Controllers
{
    [ApiController]
    [Route("leaderboard")]
    public class LeaderboardController : Controller
    {
        private readonly IScoresAndRanksService _scoresAndRanksService;
        public LeaderboardController(IScoresAndRanksService scoresAndRanksService) 
        {
            _scoresAndRanksService = scoresAndRanksService;
        }

        [HttpGet]
        public JsonResult GetByRank([Required] int start, [Required] int end) 
        {
            var customers = _scoresAndRanksService.GetByRank(start, end);
            return Json(customers);
        }

        [HttpGet("{id}")]
        public JsonResult GetById(ulong id, int? high, int? low) 
        {
            var customers = _scoresAndRanksService.GetCustomer(id, high ?? 0, low ?? 0);
            return Json(customers);
        }

    }
}
