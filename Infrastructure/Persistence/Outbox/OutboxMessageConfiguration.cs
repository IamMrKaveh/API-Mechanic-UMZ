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
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(e => e.Error)
            .HasColumnName("error")
            .HasColumnType("text")
            .HasMaxLength(2000);

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(e => e.IsPoisoned)
            .HasColumnName("is_poisoned")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.TraceParent)
            .HasColumnName("trace_parent")
            .HasMaxLength(55);

        builder.Property(e => e.TraceState)
            .HasColumnName("trace_state")
            .HasMaxLength(256);

        builder.HasIndex(e => e.CreatedAt)
            .HasFilter("\"processed_at\" IS NULL AND \"is_poisoned\" = false AND \"retry_count\" < 5")
            .HasDatabaseName("IX_OutboxMessages_Pending");

        builder.HasIndex(e => new { e.ProcessedAt, e.IsPoisoned, e.RetryCount, e.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Dispatch");
    }
}
