using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(c => c.SaleNumber).NotEmpty().MaximumLength(50);
        RuleFor(c => c.SaleDate).NotEqual(default(DateTime));
        RuleFor(c => c.CustomerId).NotEqual(Guid.Empty);
        RuleFor(c => c.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(c => c.BranchId).NotEqual(Guid.Empty);
        RuleFor(c => c.BranchName).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Items)
            .NotEmpty().WithMessage("A sale must have at least one item.");

        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEqual(Guid.Empty);
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(150);
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .LessThanOrEqualTo(SaleItem.MaxQuantity)
                .WithMessage($"Quantity must be between 1 and {SaleItem.MaxQuantity}.");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
