using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ReliableEvents.Sample.Application.Abstractions;
using ReliableEvents.Sample.Domain;

namespace ReliableEvents.Sample.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerEmail).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.OccurredAtUtc).IsRequired();
            entity.HasIndex(x => x.PublishedAtUtc);
        });
    }
}
