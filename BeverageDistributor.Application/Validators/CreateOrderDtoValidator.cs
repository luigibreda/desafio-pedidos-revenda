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
                .NotEmpty().WithMessage("Distribuidor ID é obrigatório");

            RuleFor(x => x.ClientId)
                .NotEmpty().WithMessage("Cliente ID é obrigatório")
                .MaximumLength(100).WithMessage("Cliente ID não pode exceder 100 caracteres");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Pelo menos um item de pedido é obrigatório")
                .Must(items => items != null && items.Any())
                .WithMessage("Pelo menos um item de pedido é obrigatório");

            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemDtoValidator());
        }
    }

    public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
    {
        public OrderItemDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("ID do produto é obrigatório");

            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("Nome do produto é obrigatório")
                .MaximumLength(200).WithMessage("Nome do produto não pode exceder 200 caracteres");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Preço unitário não pode ser negativo");
        }
    }
}
