using ScoresAndRanks.Models;
using ScoresAndRanks.DataType;

namespace ScoresAndRanks.Services
{
    public class SkipListService : IScoresAndRanksService
    {
        private  ConcurrentCustomerSkipList _customerList;

        public SkipListService()
        {
            _customerList = new ConcurrentCustomerSkipList();
        }

        public List<Customer> GetByRank(int start, int end)
        {
            var result = new List<Customer>();
            result = _customerList.GetByRange(start, end);
            return result;
        }

        public List<Customer> GetCustomer(ulong id, int high, int low)
        {
            var result = new List<Customer>();
            if(!_customerList.ContainsId(id)) return result;
            foreach (var customer in _customerList.GetByWindow(id, high, low))
            {
                result.Add(new Customer
                {
                    CustomerID = customer.Id,
                    Score = customer.Score,
                    Rank = customer.Rank
                });
            }
            return result;
        }

        public Customer InsertOrUpdateCustomer(Customer customer)
        {
            if (customer.Score > 1000 || customer.Score < -1000) throw new Exception("Invalid parameters.");
            return _customerList.AddOrUpdate(customer.CustomerID, customer.Score);
        }
    }
}
