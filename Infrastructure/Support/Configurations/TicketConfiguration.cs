using Domain.Support.Entities;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Support.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Domain.Support.Aggregates.Ticket>
{
    public void Configure(EntityTypeBuilder<Domain.Support.Aggregates.Ticket> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => TicketId.From(value));

        builder.Property(e => e.CustomerId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.AssignedAgentId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasConversion(s => s.Value, value => TicketStatus.FromString(value))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Priority)
            .HasConversion(p => p.Value, value => TicketPriority.FromString(value))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Category)
            .HasConversion(c => c.Value, value => TicketCategory.Create(value))
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.ResolvedAt);
        builder.Property(e => e.LastActivityAt);

        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.AssignedAgentId);
        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.Messages)
            .WithOne()
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}