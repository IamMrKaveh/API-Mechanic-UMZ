using Infrastructure.Persistence.Outbox;

namespace Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasConversion(v => v.Value, v => new OutboxMessageId(v));
        builder.Property(e => e.Type).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Payload).IsRequired().HasColumnType("text");
        builder.Property(e => e.Error).HasColumnType("text");
        builder.HasIndex(e => e.ProcessedAt);
    }
}