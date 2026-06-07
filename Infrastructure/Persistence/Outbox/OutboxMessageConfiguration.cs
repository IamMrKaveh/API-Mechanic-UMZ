namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => OutboxMessageId.From(value))
            .HasColumnName("id");

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(e => e.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(e => e.IsPoisoned)
            .HasColumnName("is_poisoned")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(e => new { e.ProcessedAt, e.IsPoisoned, e.RetryCount, e.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Dispatch");
    }
}