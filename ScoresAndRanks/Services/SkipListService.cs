using ScoresAndRanks.Models;
using ScoresAndRanks.DataType;
using ScoresAndRanks.ExceptionHandler;
using static ScoresAndRanks.ExceptionHandler.ScoresAndRanksException;

namespace ScoresAndRanks.Services
{
    public class SkipListService : IScoresAndRanksService
    {
        private  ConcurrentCustomerSkipList _customerList;

        public SkipListService()
        {
            _customerList = new ConcurrentCustomerSkipList();
        }

        public async Task<List<Customer>> GetByRankAsync(int start, int end)
        {
            var result = await Task.Run(() => { return _customerList.GetByRange(start, end); });
            return result;
        }

        public async Task<List<Customer>> GetCustomerAsync(ulong id, int high, int low)
        {
            if(!_customerList.ContainsId(id)) return new List<Customer>();
            var result = await Task.Run(() => { return _customerList.GetByWindow(id, high, low); });
            return result;
        }

        public async Task<long> InsertOrUpdateCustomerAsync(Customer customer)
        {
            if (customer.Score > 1000 || customer.Score < -1000) throw new ScoresAndRanksException(ScoresAndRanksExceptionType.SCORE_OUT_OF_RANGE);
            var task = Task.Run(() => { return _customerList.AddOrUpdate(customer.CustomerID, customer.Score); });
            var result = await task;
            return result;// _customerList.AddOrUpdate(customer.CustomerID, customer.Score);
        }

        public long InsertOrUpdateCustomer(Customer customer)
        {
            if (customer.Score > 1000 || customer.Score < -1000) throw new ScoresAndRanksException(ScoresAndRanksExceptionType.SCORE_OUT_OF_RANGE);
            return  _customerList.AddOrUpdate(customer.CustomerID, customer.Score);
        }
    }
}
