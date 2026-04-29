using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class SaleValidator : AbstractValidator<Sale>
{
    public SaleValidator()
    {
        RuleFor(s => s.SaleNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(s => s.SaleDate)
            .NotEqual(default(DateTime))
            .WithMessage("SaleDate is required.");

        RuleFor(s => s.CustomerId).NotEqual(Guid.Empty);
        RuleFor(s => s.CustomerName).NotEmpty().MaximumLength(150);

        RuleFor(s => s.BranchId).NotEqual(Guid.Empty);
        RuleFor(s => s.BranchName).NotEmpty().MaximumLength(150);

        RuleFor(s => s.Items)
            .NotEmpty()
            .WithMessage("A sale must have at least one item.");

        RuleForEach(s => s.Items).SetValidator(new SaleItemValidator());
    }
}
