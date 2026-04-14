namespace ReliableEvents.Sample.API.Contracts;

public sealed record CreateOrderRequest(string CustomerEmail, decimal TotalAmount);
