using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class SaleItemValidator : AbstractValidator<SaleItem>
{
    public SaleItemValidator()
    {
        RuleFor(i => i.ProductId).NotEqual(Guid.Empty)
            .WithMessage("ProductId is required.");

        RuleFor(i => i.ProductName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(i => i.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(SaleItem.MaxQuantity)
            .WithMessage($"Quantity must be between 1 and {SaleItem.MaxQuantity}.");

        RuleFor(i => i.UnitPrice)
            .GreaterThanOrEqualTo(0);
    }
}
