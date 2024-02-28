using ScoresAndRanks.Services;
using ScoresAndRanks.Models;

namespace ScoresAndRanksTest
{
    public class ServiceUnitTest
    {
        IScoresAndRanksService _service;

        public ServiceUnitTest() 
        {
            
        }

        /* id       score       rank
         * 87982    300         1
         * 65535    200         2
         * 1        100         3
         */
        internal void InitData()
        {
            _service = new SortedListService();
            //_service = new LinkListService();
            Customer customer1 = new Customer
            {
                Id = 1,
                Score = 100,
                Rank = 0
            };
            Customer customer2 = new Customer
            {
                Id = 65535,
                Score = 200,
                Rank = 0
            };
            Customer customer3 = new Customer
            {
                Id = 87982,
                Score = 300,
                Rank = 0
            };
            _service.InsertOrUpdateCustomer(customer1);
            _service.InsertOrUpdateCustomer(customer2);
            _service.InsertOrUpdateCustomer(customer3);
        }

        internal void AssertCustomer(Customer customers, long id, int rank)
        {
            Assert.Equal(id, customers.Id);
            Assert.Equal(rank, customers.Rank);
        }

        [Fact]
        public void TestGetById()
        {
            // Arrange
            InitData();
            Customer customer1 = new Customer
            {
                Id = 1,
                Score = 300,
                Rank = 0
            };
            //update, customer1's score should be 400
            _service.InsertOrUpdateCustomer(customer1);

            // Act
            var customers = _service.GetCustomer(87982, 1, 1);


            // Assert
            Assert.NotNull(customers);
            Assert.Equal(3, customers.Count());
            AssertCustomer(customers[0], 1, 1);
            AssertCustomer(customers[1], 87982, 2);
            AssertCustomer(customers[2], 65535, 3);

        }

        [Fact]
        public void TestGetByRank() 
        { 
            InitData();
            var customers =  _service.GetByRank(1, 3);
            Assert.NotNull(customers);
            Assert.Equal(3, customers.Count());
            AssertCustomer(customers[0], 87982, 1);
            AssertCustomer(customers[1], 65535, 2);
            AssertCustomer(customers[2], 1, 3);

        }

        [Fact]
        public void TestNegativeUpdate()
        {
            InitData();
            Customer customer = new Customer
            {
                Id = 65535,
                Score = -150,
                Rank = 0
            };
            _service.InsertOrUpdateCustomer(customer);
            var customers = _service.GetByRank(1, 3);
            Assert.NotNull(customers);
            Assert.Equal(3, customers.Count());
            AssertCustomer(customers[2], 65535, 3);

        }

        [Fact]
        public void TestEdge() 
        { 
            InitData();
            var customers = _service.GetByRank(9000, 10000);
            Assert.Empty(customers);
            Assert.Throws<Exception>(() => _service.GetByRank(10, 5));
            customers = _service.GetByRank(1, 10000);
            Assert.Equal(3, customers.Count());
            customers = _service.GetByRank(0, 3);
            Assert.Equal(3, customers.Count());

            customers = _service.GetCustomer(65535, 100, 100);
            Assert.Equal(3, customers.Count());
            customers = _service.GetCustomer(65535, -100, -100);
            Assert.Single(customers);

            customers = _service.GetCustomer(12345, 0, 0);
            Assert.Empty(customers);

            Assert.Throws<OverflowException>(() => _service.InsertOrUpdateCustomer(new Customer {
                Id = 1,
                Score = long.MaxValue,
                Rank = 1
            }));

        }
    }
}