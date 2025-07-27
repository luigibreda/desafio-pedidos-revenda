using BeverageDistributor.Domain.Exceptions;

namespace BeverageDistributor.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; private set; }
        public Order Order { get; private set; }
        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        protected OrderItem() { }

        public OrderItem(Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            if (orderId == Guid.Empty)
                throw new DomainException("Order ID is required");
                
            if (productId == Guid.Empty)
                throw new DomainException("Product ID is required");

            if (string.IsNullOrWhiteSpace(productName))
                throw new DomainException("Product name is required");

            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero");

            if (unitPrice < 0)
                throw new DomainException("Unit price cannot be negative");

            OrderId = orderId;
            ProductId = productId;
            ProductName = productName.Trim();
            Quantity = quantity;
            UnitPrice = unitPrice;
        }

        public void IncreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new DomainException("Amount must be greater than zero");

            Quantity += amount;
        }

        public void DecreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new DomainException("Amount must be greater than zero");

            if (amount >= Quantity)
                throw new DomainException("Cannot decrease quantity below zero");

            Quantity -= amount;
        }

        public void UpdateUnitPrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new DomainException("Unit price cannot be negative");

            UnitPrice = newPrice;
        }
    }
}
