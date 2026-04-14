namespace ReliableEvents.Sample.Domain;

public sealed class Order
{
    public Guid Id { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Order() { }

    public Order(Guid id, string customerEmail, decimal totalAmount)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new ArgumentException("Customer email is required.", nameof(customerEmail));
        }

        if (totalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Order total must be greater than zero.");
        }

        Id = id;
        CustomerEmail = customerEmail;
        TotalAmount = totalAmount;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
