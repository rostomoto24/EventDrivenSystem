using System.Text.Json;
using MediatR;
using ReliableEvents.Sample.Application.Abstractions;
using ReliableEvents.Sample.Domain;

namespace ReliableEvents.Sample.Application.Orders;

public sealed class CreateOrderCommandHandler(IAppDbContext dbContext) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid(), request.CustomerEmail, request.TotalAmount);
        var integrationEvent = new OrderCreatedIntegrationEvent(order.Id, order.CustomerEmail, order.TotalAmount, order.CreatedAtUtc);
        var outboxMessage = new OutboxMessage(
            Guid.NewGuid(),
            type: nameof(OrderCreatedIntegrationEvent),
            payload: JsonSerializer.Serialize(integrationEvent));

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);
        await dbContext.AddOrderAsync(order, cancellationToken);
        await dbContext.AddOutboxMessageAsync(outboxMessage, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return order.Id;
    }
}
