using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Enums;
using BeverageDistributor.Domain.Exceptions;
using Xunit;

namespace BeverageDistributor.Tests.Domain.Entities
{
    public class OrderTests
    {
        private readonly Guid _distributorId = Guid.NewGuid();
        private readonly string _clientId = "client123";
        private readonly Guid _productId = Guid.NewGuid();
        private const string ProductName = "Test Product";
        private const int Quantity = 10;
        private const decimal UnitPrice = 10.50m;

        [Fact]
        public void UpdateStatus_WithValidStatus_UpdatesStatusCorrectly()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);

            order.UpdateStatus("processing");
            
            Assert.Equal(OrderStatus.Processing, order.Status);

            order.UpdateStatus("completed");
            
            Assert.Equal(OrderStatus.Completed, order.Status);
        }

        [Fact]
        public void UpdateStatus_WithCancelStatus_CancelsOrder()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);

            order.UpdateStatus("cancelled");
            
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        [Fact]
        public void UpdateStatus_WithPendingStatus_WhenAlreadyPending_DoesNotThrow()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);

            order.UpdateStatus("pending");
            Assert.Equal(OrderStatus.Pending, order.Status);
        }

        [Fact]
        public void UpdateStatus_WithPendingStatus_WhenNotPending_ThrowsException()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);
            order.UpdateStatus("processing");
            var exception = Assert.Throws<DomainException>(() => order.UpdateStatus("pending"));
            Assert.Equal("Cannot set status back to Pending", exception.Message);
        }

        [Fact]
        public void UpdateStatus_WithInvalidStatus_ThrowsException()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);

            var exception = Assert.Throws<DomainException>(() => order.UpdateStatus("invalid_status"));
            Assert.StartsWith("Invalid status:", exception.Message);
        }

        [Fact]
        public void UpdateStatus_WithNullStatus_ThrowsException()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);

            var exception = Assert.Throws<DomainException>(() => order.UpdateStatus(null));
            Assert.Equal("Status is required", exception.Message);
        }

        [Fact]
        public void UpdateStatus_WithEmptyStatus_ThrowsException()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);

            var exception = Assert.Throws<DomainException>(() => order.UpdateStatus(""));
            Assert.Equal("Status is required", exception.Message);
        }

        [Fact]
        public void UpdateStatus_WithWhitespaceStatus_ThrowsException()
        {
            var order = new Order(_distributorId, _clientId);
            order.AddItem(_productId, ProductName, Quantity, UnitPrice);

            var exception = Assert.Throws<DomainException>(() => order.UpdateStatus("   "));
            Assert.Equal("Status is required", exception.Message);
        }
    }
}
