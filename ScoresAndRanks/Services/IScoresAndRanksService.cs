using ScoresAndRanks.Models;

namespace ScoresAndRanks.Services
{
    public interface IScoresAndRanksService
    {
        public long InsertOrUpdateCustomer(Customer customer);
        public List<Customer> GetByRank(int start, int end);
        public List<Customer> GetCustomer(ulong id, int high, int low);
    }
}
