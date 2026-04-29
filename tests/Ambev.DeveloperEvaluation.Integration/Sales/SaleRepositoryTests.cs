using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

/// <summary>
/// Integration tests for <see cref="SaleRepository"/> using the EF Core
/// in-memory provider. Wildcard string filters round-trip via plain
/// <c>Contains</c>/<c>StartsWith</c>/<c>EndsWith</c> on the in-memory
/// provider; against PostgreSQL Npgsql translates them to ILike.
/// </summary>
public class SaleRepositoryTests : IDisposable
{
    private readonly DefaultContext _context;
    private readonly SaleRepository _repository;

    public SaleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: $"sales-{Guid.NewGuid()}")
            .Options;

        _context = new DefaultContext(options);
        _repository = new SaleRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "Create persists sale with items and unique sale number")]
    public async Task CreateAsync_PersistsSaleWithItems()
    {
        var sale = BuildSale("S-001");
        sale.AddItem(Guid.NewGuid(), "Beer", 5, 10m);
        sale.AddItem(Guid.NewGuid(), "Snack", 2, 7m);

        await _repository.CreateAsync(sale);

        var loaded = await _repository.GetByIdAsync(sale.Id);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Items.Count);
        Assert.Equal("S-001", loaded.SaleNumber);
    }

    [Fact(DisplayName = "GetBySaleNumber returns the persisted sale")]
    public async Task GetBySaleNumberAsync_ReturnsSale()
    {
        var sale = BuildSale("S-NUM");
        sale.AddItem(Guid.NewGuid(), "Beer", 1, 5m);
        await _repository.CreateAsync(sale);

        var loaded = await _repository.GetBySaleNumberAsync("S-NUM");

        Assert.NotNull(loaded);
        Assert.Equal(sale.Id, loaded!.Id);
    }

    [Fact(DisplayName = "Delete removes sale")]
    public async Task DeleteAsync_RemovesSale()
    {
        var sale = BuildSale("S-DEL");
        sale.AddItem(Guid.NewGuid(), "Beer", 1, 5m);
        await _repository.CreateAsync(sale);

        var result = await _repository.DeleteAsync(sale.Id);

        Assert.True(result);
        Assert.Null(await _repository.GetByIdAsync(sale.Id));
    }

    [Fact(DisplayName = "List honours pagination and CustomerId filter")]
    public async Task ListAsync_FiltersAndPaginates()
    {
        var customerId = Guid.NewGuid();
        for (var i = 0; i < 12; i++)
        {
            var sale = BuildSale($"S-{i:D3}", customerId);
            sale.AddItem(Guid.NewGuid(), "Beer", 1, 5m);
            await _repository.CreateAsync(sale);
        }

        var (items, totalCount) = await _repository.ListAsync(BuildQuery(
            page: 2,
            pageSize: 5,
            orderBy: "saleNumber",
            customerId: customerId));

        Assert.Equal(12, totalCount);
        Assert.Equal(5, items.Count);
    }

    [Fact(DisplayName = "List supports wildcard filtering on CustomerName")]
    public async Task ListAsync_CustomerNameWildcard_FiltersResults()
    {
        await _repository.CreateAsync(BuildSale("S-A", customerName: "Alice"));
        await _repository.CreateAsync(BuildSale("S-B", customerName: "Alex"));
        await _repository.CreateAsync(BuildSale("S-C", customerName: "Bob"));

        var (items, totalCount) = await _repository.ListAsync(BuildQuery(customerName: "Al*"));

        Assert.Equal(2, totalCount);
        Assert.All(items, s => Assert.StartsWith("Al", s.CustomerName));
    }

    [Fact(DisplayName = "List filters by total amount range")]
    public async Task ListAsync_TotalAmountRange_FiltersResults()
    {
        var lowSale = BuildSale("S-LOW");
        lowSale.AddItem(Guid.NewGuid(), "Beer", 1, 5m); // total 5
        await _repository.CreateAsync(lowSale);

        var highSale = BuildSale("S-HIGH");
        highSale.AddItem(Guid.NewGuid(), "Beer", 5, 100m); // total 450 (10% off)
        await _repository.CreateAsync(highSale);

        var (items, totalCount) = await _repository.ListAsync(BuildQuery(minTotalAmount: 100));

        Assert.Equal(1, totalCount);
        Assert.Equal("S-HIGH", items.Single().SaleNumber);
    }

    private static Sale BuildSale(string number, Guid? customerId = null, string customerName = "Customer") => new()
    {
        SaleNumber = number,
        SaleDate = DateTime.UtcNow,
        CustomerId = customerId ?? Guid.NewGuid(),
        CustomerName = customerName,
        BranchId = Guid.NewGuid(),
        BranchName = "Branch"
    };

    private static SaleListQuery BuildQuery(
        int page = 1,
        int pageSize = 10,
        string? orderBy = null,
        Guid? customerId = null,
        Guid? branchId = null,
        string? saleNumber = null,
        string? customerName = null,
        string? branchName = null,
        DateTime? minSaleDate = null,
        DateTime? maxSaleDate = null,
        decimal? minTotalAmount = null,
        decimal? maxTotalAmount = null,
        bool? isCancelled = null)
        => new(page, pageSize, orderBy, customerId, branchId, saleNumber, customerName, branchName,
            minSaleDate, maxSaleDate, minTotalAmount, maxTotalAmount, isCancelled);
}
