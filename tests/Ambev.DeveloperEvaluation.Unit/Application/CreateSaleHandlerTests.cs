using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly IDomainEventPublisher _publisher;
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _repository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _publisher = Substitute.For<IDomainEventPublisher>();
        _handler = new CreateSaleHandler(_repository, _mapper, _publisher);
    }

    [Fact(DisplayName = "Valid command creates the sale, publishes event and returns mapped result")]
    public async Task Handle_ValidCommand_CreatesSaleAndPublishesEvent()
    {
        var command = SaleTestData.GenerateValidCreateCommand(itemCount: 2, quantityPerItem: 5);
        _repository.GetBySaleNumberAsync(command.SaleNumber).Returns((Sale?)null);
        _repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Sale>());
        _mapper.Map<SaleResult>(Arg.Any<Sale>()).Returns(new SaleResult());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        await _repository.Received(1).CreateAsync(
            Arg.Is<Sale>(s => s.SaleNumber == command.SaleNumber && s.Items.Count == 2),
            Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Any<SaleCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Existing sale number throws InvalidOperationException")]
    public async Task Handle_DuplicateSaleNumber_Throws()
    {
        var command = SaleTestData.GenerateValidCreateCommand();
        _repository.GetBySaleNumberAsync(command.SaleNumber).Returns(new Sale { SaleNumber = command.SaleNumber });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
        await _repository.DidNotReceive().CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Invalid command throws ValidationException")]
    public async Task Handle_InvalidCommand_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _handler.Handle(new CreateSaleCommand(), CancellationToken.None));
    }

    [Fact(DisplayName = "Quantity above 20 surfaces a validation exception")]
    public async Task Handle_QuantityAboveLimit_ThrowsValidationException()
    {
        var command = SaleTestData.GenerateValidCreateCommand();
        command.Items[0].Quantity = 30;

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
