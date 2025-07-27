using System;
using System.Collections.Generic;
using System.Linq;
using BeverageDistributor.Domain.Enums;
using BeverageDistributor.Domain.Exceptions;
using BeverageDistributor.Domain.ValueObjects;

namespace BeverageDistributor.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid DistributorId { get; private set; }
        public Distributor Distributor { get; private set; }
        public string ClientId { get; private set; }
        public OrderStatus Status { get; private set; } = OrderStatus.Pending;
        public DateTime OrderDate { get; private set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; private set; }
        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        protected Order() { }

        public Order(Guid distributorId, string clientId)
        {
            if (distributorId == Guid.Empty)
                throw new DomainException("Distributor ID is required");

            if (string.IsNullOrWhiteSpace(clientId))
                throw new DomainException("Client ID is required");

            DistributorId = distributorId;
            ClientId = clientId.Trim();
        }

        public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Cannot modify order after it's been processed");

            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero");

            if (unitPrice < 0)
                throw new DomainException("Unit price cannot be negative");

            var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.IncreaseQuantity(quantity);
            }
            else
            {
                var item = new OrderItem(Id, productId, productName, quantity, unitPrice);
                _items.Add(item);
            }

            UpdateTotalAmount();
        }

        public void RemoveItem(Guid productId, int quantity = 0)
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Cannot modify order after it's been processed");

            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                throw new DomainException("Item not found in order");

            if (quantity <= 0 || quantity >= item.Quantity)
            {
                _items.Remove(item);
            }
            else
            {
                item.DecreaseQuantity(quantity);
            }

            UpdateTotalAmount();
        }

        public void Process()
        {
            if (Status != OrderStatus.Pending)
                throw new DomainException("Order has already been processed");

            if (!_items.Any())
                throw new DomainException("Cannot process an empty order");

            Status = OrderStatus.Processing;
        }

        public void Complete()
        {
            if (Status != OrderStatus.Processing)
                throw new DomainException("Order must be in Processing status to be completed");

            Status = OrderStatus.Completed;
        }

        public void Cancel()
        {
            if (Status == OrderStatus.Completed || Status == OrderStatus.Cancelled)
                throw new DomainException("Cannot cancel an order that is already completed or cancelled");

            Status = OrderStatus.Cancelled;
        }

        private void UpdateTotalAmount()
        {
            TotalAmount = _items.Sum(i => i.TotalPrice);
        }
    }
}
