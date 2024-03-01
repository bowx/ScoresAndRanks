using Microsoft.AspNetCore.Mvc;
using ScoresAndRanks.Models;
using ScoresAndRanks.Services;
using System.Diagnostics;

namespace ScoresAndRanks.Controllers
{
    /// <summary>
    /// This controll is only for testing
    /// </summary>
    public class TestingController : Controller
    {
        private readonly IScoresAndRanksService _scoresAndRanksService;

        public TestingController(IScoresAndRanksService scoresAndRanksService)
        {
            _scoresAndRanksService = scoresAndRanksService;
        }

        /// <summary>
        /// Create 1000000 of customers
        /// </summary>
        /// <returns></returns>
        [HttpGet("/load")]
        public JsonResult LoadTest()
        {

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (ulong i = 0; i <= 1000000; i++)
            {
                Customer customer = new Customer
                {
                    CustomerID = i,
                    Score = /*1100000 - i,*/ new Random().Next(10, 900000),//Insert from behind is more faster than randomly insert
                    Rank = 0
                };
                _scoresAndRanksService.InsertOrUpdateCustomer(customer);
            }
            sw.Stop();
            var time = new { totalTime = sw.Elapsed.TotalMicroseconds };
            return new JsonResult(time);
        }

        /// <summary>
        /// Used for Netling test, randomly create with GET method
        /// </summary>
        /// <returns></returns>
        [HttpGet("/create")]
        public ActionResult<int> RandomUpdateAndCreate()
        {
            Customer customer = new Customer {
                CustomerID = (ulong)new Random().Next(1000000, 5000000),
                Score = new Random().Next(10, 900000),
                Rank = 0

            };
            _scoresAndRanksService.InsertOrUpdateCustomer(customer);
            return new ActionResult<int>(0);
        }
    }
}
