namespace ReliableEvents.Sample.Application.Orders;

public sealed record OrderCreatedIntegrationEvent(Guid OrderId, string CustomerEmail, decimal TotalAmount, DateTime CreatedAtUtc);
