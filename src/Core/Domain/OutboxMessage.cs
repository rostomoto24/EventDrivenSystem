namespace ReliableEvents.Sample.Domain;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid id, string type, string payload)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public void MarkPublished(DateTime publishedAtUtc)
    {
        PublishedAtUtc = publishedAtUtc;
    }
}
