using MediatR;

namespace ReliableEvents.Sample.Application.Orders;

public sealed record CreateOrderCommand(string CustomerEmail, decimal TotalAmount) : IRequest<Guid>;
