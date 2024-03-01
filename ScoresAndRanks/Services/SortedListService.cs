using Microsoft.AspNetCore.Mvc.Formatters;
using ScoresAndRanks.Models;
using ScoresAndRanks.DataType;

namespace ScoresAndRanks.Services
{
    public class SortedListService : IScoresAndRanksService
    {
        private ConcurrentCustomerSortedList _customerList;

        private static object _lock = new object();
        public SortedListService() 
        {
            _customerList = new ConcurrentCustomerSortedList();
        }

        public List<Customer> GetByRank(int start, int end)
        {
            var result = new List<Customer>();
            foreach (var kv in _customerList.GetByRange(start, end))
            {
                result.Add(new Customer
                {
                    CustomerID = kv.Value.Id,
                    Score = kv.Value.Score,
                    Rank = kv.Key
                });
            }

            return result;
        }

        public List<Customer> GetCustomer(ulong id, int high, int low)
        {

            if (!_customerList.ContainsId(id)) return new List<Customer>();
            var result = new List<Customer>();
            foreach (var kv in _customerList.GetByWindow(id, high, low))
            {
                result.Add(new Customer
                {
                    CustomerID = kv.Value.Id,
                    Score = kv.Value.Score,
                    Rank = kv.Key
                });
            }
            return result;
        }

        public Customer InsertOrUpdateCustomer(Customer customer)
        {
            if (customer.Score > 1000 || customer.Score < -1000) throw new Exception("Invalid parameters.");
            return _customerList.AddOrUpdate(new ConcurrentCustomerSortedList.IdScoreStruct { Id = customer.CustomerID, Score = customer.Score });
        }

    }
}
