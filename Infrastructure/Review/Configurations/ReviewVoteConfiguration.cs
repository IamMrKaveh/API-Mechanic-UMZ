using Domain.Review.Entities;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.Configurations;

public sealed class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.ToTable("ReviewVotes");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasConversion(v => v.Value, v => ReviewVoteId.From(v));

        builder.Property(v => v.ReviewId)
            .HasConversion(v => v.Value, v => ReviewId.From(v))
            .IsRequired();

        builder.Property(v => v.UserId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(v => v.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt);

        builder.HasIndex(v => new { v.ReviewId, v.UserId })
            .IsUnique()
            .HasDatabaseName("IX_ReviewVotes_ReviewId_UserId_Unique");

        builder.HasIndex(v => v.ReviewId);
    }
}
