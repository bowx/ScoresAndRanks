using Microsoft.AspNetCore.Mvc;
using ScoresAndRanks.Models;
using ScoresAndRanks.Services;
using System.Diagnostics;

namespace ScoresAndRanks.Controllers
{
    [ApiController]
    [Route("customer")]
    public class CustomerController : Controller
    {
        private readonly IScoresAndRanksService _scoresAndRanksService;

        public CustomerController(IScoresAndRanksService scoresAndRanksService)
        {
            _scoresAndRanksService = scoresAndRanksService;
        }

        [HttpPost("/customer/{customerId}/score/{score?}")]
        public JsonResult UpdateAndCreate(ulong customerId, long? score )
        {
            score = score ?? 0;
            var newScore = _scoresAndRanksService.InsertOrUpdateCustomer(new Customer { CustomerID = customerId, Score = (long)score});
            return Json(new { score = newScore });
        }

        [HttpPost("/customer/{customerId}")]
        public JsonResult UpdateAndCreate(ulong customerId)
        {
            long score = 0;
            var newScore = _scoresAndRanksService.InsertOrUpdateCustomer(new Customer { CustomerID = customerId, Score = score });
            return Json(new { score = newScore });
        }

    }
}
