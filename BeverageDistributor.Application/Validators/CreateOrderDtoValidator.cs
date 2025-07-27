using BeverageDistributor.Application.DTOs.Order;
using FluentValidation;
using System.Linq;

namespace BeverageDistributor.Application.Validators
{
    public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
    {
        public CreateOrderDtoValidator()
        {
            RuleFor(x => x.DistributorId)
                .NotEmpty().WithMessage("Distributor ID is required");

            RuleFor(x => x.ClientId)
                .NotEmpty().WithMessage("Client ID is required")
                .MaximumLength(100).WithMessage("Client ID cannot exceed 100 characters");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one order item is required")
                .Must(items => items != null && items.Any())
                .WithMessage("At least one order item is required");

            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemDtoValidator());
        }
    }

    public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
    {
        public OrderItemDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");
        }
    }
}
