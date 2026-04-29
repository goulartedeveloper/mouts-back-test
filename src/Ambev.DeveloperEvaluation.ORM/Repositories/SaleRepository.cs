using System.Linq.Expressions;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default)
    {
        return _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        // The handler typically loaded `sale` via GetByIdAsync (tracked) and mutated it.
        // For the "replace" semantics we use here (delete then re-add the aggregate)
        // we must first detach the input — otherwise EF's identity map returns the
        // same instance below as `existing`, and Remove+Add on the same reference
        // collapses into a single Modified state with the items inserted under an
        // already-existing PK -> "duplicate key value violates PK_Sales".
        DetachGraph(sale);

        var existing = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sale.Id, cancellationToken);

        if (existing != null)
        {
            _context.Sales.Remove(existing);
            // Flush the delete first so the insert below doesn't race the unique PK.
            await _context.SaveChangesAsync(cancellationToken);
        }

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyCollection<Sale> Items, int TotalCount)> ListAsync(
        SaleListQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Sale> source = _context.Sales
            .AsNoTracking()
            .Include(s => s.Items);

        if (query.CustomerId.HasValue)
            source = source.Where(s => s.CustomerId == query.CustomerId.Value);

        if (query.BranchId.HasValue)
            source = source.Where(s => s.BranchId == query.BranchId.Value);

        source = ApplyStringFilter(source, query.SaleNumber, s => s.SaleNumber);
        source = ApplyStringFilter(source, query.CustomerName, s => s.CustomerName);
        source = ApplyStringFilter(source, query.BranchName, s => s.BranchName);

        if (query.MinSaleDate.HasValue)
            source = source.Where(s => s.SaleDate >= query.MinSaleDate.Value);

        if (query.MaxSaleDate.HasValue)
            source = source.Where(s => s.SaleDate <= query.MaxSaleDate.Value);

        if (query.MinTotalAmount.HasValue)
            source = source.Where(s => s.TotalAmount >= query.MinTotalAmount.Value);

        if (query.MaxTotalAmount.HasValue)
            source = source.Where(s => s.TotalAmount <= query.MaxTotalAmount.Value);

        if (query.IsCancelled.HasValue)
        {
            var status = query.IsCancelled.Value ? SaleStatus.Cancelled : SaleStatus.Active;
            source = source.Where(s => s.Status == status);
        }

        source = ApplyOrdering(source, query.OrderBy);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private void DetachGraph(Sale sale)
    {
        var entry = _context.Entry(sale);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;

        foreach (var item in sale.Items)
        {
            var itemEntry = _context.Entry(item);
            if (itemEntry.State != EntityState.Detached)
                itemEntry.State = EntityState.Detached;
        }
    }

    private static IQueryable<Sale> ApplyStringFilter(
        IQueryable<Sale> source,
        string? rawValue,
        Expression<Func<Sale, string>> selector)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return source;

        var raw = rawValue.Trim();
        var startsWildcard = raw.StartsWith('*');
        var endsWildcard = raw.EndsWith('*');
        var trimmed = raw.Trim('*');

        // Plain Contains/StartsWith/EndsWith keeps the query provider-agnostic (works on
        // the InMemory provider used by tests) and Npgsql still translates them to ILike.
        var parameter = selector.Parameters[0];
        var body = selector.Body;
        var constant = Expression.Constant(trimmed);

        Expression compare = (startsWildcard, endsWildcard) switch
        {
            (true, true) => Expression.Call(body, nameof(string.Contains), Type.EmptyTypes, constant),
            (true, false) => Expression.Call(body, nameof(string.EndsWith), Type.EmptyTypes, constant),
            (false, true) => Expression.Call(body, nameof(string.StartsWith), Type.EmptyTypes, constant),
            _ => Expression.Equal(body, constant)
        };

        return source.Where(Expression.Lambda<Func<Sale, bool>>(compare, parameter));
    }

    private static IQueryable<Sale> ApplyOrdering(IQueryable<Sale> source, string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return source.OrderByDescending(s => s.SaleDate);

        IOrderedQueryable<Sale>? ordered = null;

        foreach (var rawClause in order.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = rawClause.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var field = parts[0].Trim().Trim('"');
            var desc = parts.Length > 1 && parts[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

            ordered = ApplyOrderClause(ordered, source, field, desc);
        }

        return ordered ?? source.OrderByDescending(s => s.SaleDate);
    }

    private static IOrderedQueryable<Sale> ApplyOrderClause(
        IOrderedQueryable<Sale>? current,
        IQueryable<Sale> source,
        string field,
        bool desc)
    {
        return field.ToLowerInvariant() switch
        {
            "salenumber" => Apply(current, source, s => s.SaleNumber, desc),
            "saledate" => Apply(current, source, s => s.SaleDate, desc),
            "customername" => Apply(current, source, s => s.CustomerName, desc),
            "branchname" => Apply(current, source, s => s.BranchName, desc),
            "totalamount" => Apply(current, source, s => s.TotalAmount, desc),
            "status" => Apply(current, source, s => s.Status, desc),
            "createdat" => Apply(current, source, s => s.CreatedAt, desc),
            _ => current ?? source.OrderByDescending(s => s.SaleDate)
        };
    }

    private static IOrderedQueryable<Sale> Apply<TKey>(
        IOrderedQueryable<Sale>? current,
        IQueryable<Sale> source,
        Expression<Func<Sale, TKey>> selector,
        bool desc)
    {
        if (current == null)
            return desc ? source.OrderByDescending(selector) : source.OrderBy(selector);

        return desc ? current.ThenByDescending(selector) : current.ThenBy(selector);
    }
}
