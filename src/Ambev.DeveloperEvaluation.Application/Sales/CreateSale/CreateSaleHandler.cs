using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _eventPublisher;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IDomainEventPublisher eventPublisher)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var existing = await _saleRepository.GetBySaleNumberAsync(command.SaleNumber, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Sale with number {command.SaleNumber} already exists.");

        var sale = new Sale
        {
            SaleNumber = command.SaleNumber,
            SaleDate = DateTime.SpecifyKind(command.SaleDate, DateTimeKind.Utc),
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName
        };

        foreach (var input in command.Items)
            sale.AddItem(input.ProductId, input.ProductName, input.Quantity, input.UnitPrice);

        var created = await _saleRepository.CreateAsync(sale, cancellationToken);
        await _eventPublisher.PublishAsync(new SaleCreatedEvent(created), cancellationToken);

        return _mapper.Map<SaleResult>(created);
    }
}
