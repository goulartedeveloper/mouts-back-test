using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>
/// Bogus-based factory for Sale domain test data.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    public static Sale GenerateValidSale(int itemCount = 1)
    {
        var sale = new Sale
        {
            SaleNumber = $"S-{Faker.Random.AlphaNumeric(8).ToUpper()}",
            SaleDate = DateTime.UtcNow.AddDays(-Faker.Random.Int(0, 30)),
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Name.FullName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Company.CompanyName()
        };

        for (var i = 0; i < itemCount; i++)
        {
            sale.AddItem(
                Guid.NewGuid(),
                Faker.Commerce.ProductName(),
                Faker.Random.Int(1, 3),
                decimal.Parse(Faker.Commerce.Price(1, 100)));
        }

        return sale;
    }

    public static CreateSaleCommand GenerateValidCreateCommand(int itemCount = 1, int quantityPerItem = 1)
    {
        return new CreateSaleCommand
        {
            SaleNumber = $"S-{Faker.Random.AlphaNumeric(8).ToUpper()}",
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = Faker.Name.FullName(),
            BranchId = Guid.NewGuid(),
            BranchName = Faker.Company.CompanyName(),
            Items = Enumerable.Range(0, itemCount).Select(_ => new SaleItemInput
            {
                ProductId = Guid.NewGuid(),
                ProductName = Faker.Commerce.ProductName(),
                Quantity = quantityPerItem,
                UnitPrice = decimal.Parse(Faker.Commerce.Price(1, 100))
            }).ToList()
        };
    }
}
