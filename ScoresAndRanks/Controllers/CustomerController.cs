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

        [HttpPost("/customer/{customerId}/score/{score}")]
        public ActionResult UpdateAndCreate(long customerId, long score )
        {
            _scoresAndRanksService.InsertOrUpdateCustomer(new Customer { CustomerID = customerId, Score = score});    
            return NoContent();
        }

        
    }
}
