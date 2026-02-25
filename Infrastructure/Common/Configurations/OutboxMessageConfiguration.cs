namespace Infrastructure.Common.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.OccurredAt).IsRequired();
        builder.Property(o => o.ProcessedAt);
        builder.Property(o => o.Error).HasMaxLength(2000);

        builder.HasIndex(o => o.ProcessedAt);
    }
}