using ScoresAndRanks.Models;

namespace ScoresAndRanks.Services
{
    public interface IScoresAndRanksService
    {
        public void InsertOrUpdateCustomer(Customer customer);
        public List<Customer> GetByRank(int start, int end);
        public List<Customer> GetCustomer(long id, int high, int low);
    }
}
