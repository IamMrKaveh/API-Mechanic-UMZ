using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Support.Configurations;

internal sealed class TicketMessageConfiguration : IEntityTypeConfiguration<Domain.Support.Entities.TicketMessage>
{
    public void Configure(EntityTypeBuilder<Domain.Support.Entities.TicketMessage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => TicketMessageId.From(value));

        builder.Property(e => e.TicketId)
            .HasConversion(id => id.Value, value => TicketId.From(value))
            .IsRequired();

        builder.Property(e => e.SenderId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.SenderType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Content)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(e => e.IsEdited).IsRequired();
        builder.Property(e => e.EditedAt);
        builder.Property(e => e.SentAt).IsRequired();

        builder.HasIndex(e => e.TicketId);
    }
}