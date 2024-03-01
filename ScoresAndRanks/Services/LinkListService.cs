using ScoresAndRanks.Models;
using ScoresAndRanks.DataType;

namespace ScoresAndRanks.Services
{
    public class LinkListService : IScoresAndRanksService
    {
        private  ConcurrentCustomerLinkList _customerList;

        public LinkListService()
        {
            _customerList = new ConcurrentCustomerLinkList();
        }

        public List<Customer> GetByRank(int start, int end)
        {
            var result = new List<Customer>();
            foreach (var customer in _customerList.GetByRange(start, end))
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

        public List<Customer> GetCustomer(long id, int high, int low)
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

        public void InsertOrUpdateCustomer(Customer customer)
        {
            _customerList.AddOrUpdate(customer.CustomerID, customer.Score);
        }
    }
}
