namespace Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Content).IsRequired().HasColumnType("text");
        builder.Property(e => e.Error).HasColumnType("text");
    }
}