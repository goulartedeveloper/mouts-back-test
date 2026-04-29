using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Repository contract for the <see cref="Sale"/> aggregate.
/// </summary>
public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);

    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists sales applying optional filters, ordering and pagination.
    /// </summary>
    Task<(IReadOnlyCollection<Sale> Items, int TotalCount)> ListAsync(
        SaleListQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter / pagination / ordering parameters for <see cref="ISaleRepository.ListAsync"/>.
/// String fields accept the <c>*</c> wildcard described in <c>/.doc/general-api.md</c>.
/// </summary>
public sealed record SaleListQuery(
    int Page,
    int PageSize,
    string? OrderBy,
    Guid? CustomerId,
    Guid? BranchId,
    string? SaleNumber,
    string? CustomerName,
    string? BranchName,
    DateTime? MinSaleDate,
    DateTime? MaxSaleDate,
    decimal? MinTotalAmount,
    decimal? MaxTotalAmount,
    bool? IsCancelled);
