namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxArchiveMessageConfiguration : IEntityTypeConfiguration<OutboxArchiveMessage>
{
    public void Configure(EntityTypeBuilder<OutboxArchiveMessage> builder)
    {
        builder.ToTable("OutboxMessagesArchive");

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
            .HasColumnName("processed_at")
            .IsRequired();

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

        builder.Property(e => e.ArchivedAt)
            .HasColumnName("archived_at")
            .IsRequired();

        builder.HasIndex(e => e.ArchivedAt)
            .HasDatabaseName("IX_OutboxMessagesArchive_ArchivedAt");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_OutboxMessagesArchive_CreatedAt");
    }
}
