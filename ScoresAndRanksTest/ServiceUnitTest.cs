using ScoresAndRanks.Services;
using ScoresAndRanks.Models;
using ScoresAndRanks.ExceptionHandler;

namespace ScoresAndRanksTest
{
    public class ServiceUnitTest
    {
        IScoresAndRanksService _service;

        public ServiceUnitTest() 
        {
            
        }
        
        internal void InitData()
        {
            //_service = new SortedListService();
            _service = new SkipListService();

            CreateCustomer(15514665, 124);
            CreateCustomer(81546541, 113);
            CreateCustomer(1745431, 100);
            CreateCustomer(76786448, 100);
            CreateCustomer(254814111, 96);
            CreateCustomer(53274324, 95);
            CreateCustomer(6144320, 93);
            CreateCustomer(8009471, 93);
            CreateCustomer(11028481, 93);
            CreateCustomer(38819, 92);
        }

        internal void CreateCustomer(ulong id, long score)
        {
            _service.InsertOrUpdateCustomerAsync(new Customer {
                CustomerID= id,
                Score = score
            });
        }

        internal void AssertCustomer(Customer customers, ulong id, int rank)
        {
            Assert.Equal(id, customers.CustomerID);
            Assert.Equal(rank, customers.Rank);
        }

        [Fact]
        public async void TestGetById()
        {
            // Arrange
            InitData();
            Customer customer1 = new Customer
            {
                CustomerID = 254814111,
                Score = 300,
                Rank = 0
            };
            //update, customer1's score should be 396
            await _service.InsertOrUpdateCustomerAsync(customer1);

            // Act
            var customers = await _service.GetCustomerAsync(254814111, 1, 3);

            // Assert
            Assert.NotNull(customers);
            Assert.Equal(4, customers.Count());
            AssertCustomer(customers[0], 254814111, 1);
            AssertCustomer(customers[1], 15514665, 2);
            AssertCustomer(customers[2], 81546541, 3);

        }

        [Fact]
        public async void TestGetByRank() 
        { 
            InitData();
            var customers = await _service.GetByRankAsync(2, 4);
            Assert.NotNull(customers);
            Assert.Equal(3, customers.Count());
            AssertCustomer(customers[0], 81546541, 2);
            AssertCustomer(customers[1], 1745431, 3);
            AssertCustomer(customers[2], 76786448, 4);

        }

        [Fact]
        public async void TestNegativeUpdate()
        {
            InitData();
            Customer customer = new Customer
            {
                CustomerID = 15514665,
                Score = -25,
                Rank = 0
            };
            await _service.InsertOrUpdateCustomerAsync(customer);
            var customers = await _service.GetByRankAsync(1, 4);
            Assert.Equal(4, customers.Count());
            AssertCustomer(customers[3], 15514665, 4);
            //if the customer's score is 0 or below, it should be removed from the list
            customer.Score = -100;
            await _service.InsertOrUpdateCustomerAsync(customer);
            var customers2 = await _service.GetByRankAsync(1, 10);
            Assert.Equal(9, customers2.Count());

        }

        [Fact]
        public async void TestEdge() 
        { 
            InitData();
            var customers = await _service.GetByRankAsync(9000, 10000);
            Assert.Empty(customers);
            await Assert.ThrowsAsync<ScoresAndRanksException>(async () => await _service.GetByRankAsync(10, 5));
            customers = await _service.GetByRankAsync(1, 10000);
            Assert.Equal(10, customers.Count());
            customers = await _service.GetByRankAsync(0, 3);
            Assert.Equal(3, customers.Count());

            customers = await _service.GetCustomerAsync(254814111, 100, 100);
            Assert.Equal(10, customers.Count());
            customers =  await _service.GetCustomerAsync(254814111, -100, -100);
            Assert.Single(customers);

            customers = await _service.GetCustomerAsync(12345, 0, 0);
            Assert.Empty(customers);

            await Assert.ThrowsAsync<ScoresAndRanksException>(async () => await _service.InsertOrUpdateCustomerAsync(new Customer {
                CustomerID = 1,
                Score = 1001,
                Rank = 1
            }));
            await Assert.ThrowsAsync<ScoresAndRanksException>(async () => await _service.InsertOrUpdateCustomerAsync(new Customer
            {
                CustomerID = 1,
                Score = -1001,
                Rank = 1
            }));

        }
    }
}