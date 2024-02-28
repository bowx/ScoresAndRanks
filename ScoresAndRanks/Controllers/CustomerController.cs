using Microsoft.AspNetCore.Mvc;
using ScoresAndRanks.Models;
using ScoresAndRanks.Services;
using System.Diagnostics;

namespace ScoresAndRanks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : Controller
    {
        private readonly IScoresAndRanksService _scoresAndRanksService;

        public CustomerController(IScoresAndRanksService scoresAndRanksService)
        {
            _scoresAndRanksService = scoresAndRanksService;
        }

        [HttpPost]
        public ActionResult UpdateAndCreate(Customer customer )
        {
            _scoresAndRanksService.InsertOrUpdateCustomer(customer);    
            return NoContent();
        }

        
    }
}
