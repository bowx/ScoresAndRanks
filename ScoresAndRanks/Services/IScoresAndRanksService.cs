using ScoresAndRanks.Models;

namespace ScoresAndRanks.Services
{
    public interface IScoresAndRanksService
    {
        public Task<long> InsertOrUpdateCustomerAsync(Customer customer);
        public long InsertOrUpdateCustomer(Customer customer);
        public Task<List<Customer>> GetByRankAsync(int start, int end);
        public Task<List<Customer>> GetCustomerAsync(ulong id, int high, int low);
    }
}
