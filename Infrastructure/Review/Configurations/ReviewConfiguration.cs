using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => ReviewId.From(v));

        builder.Property(e => e.ProductId)
            .HasConversion(v => v.Value, v => ProductId.From(v))
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(e => e.OrderId)
            .HasConversion(v => v!.Value, v => OrderId.From(v));

        builder.OwnsOne(e => e.Rating, r =>
        {
            r.Property(x => x.Value)
                .HasColumnName("Rating")
                .IsRequired();
        });

        builder.OwnsOne(e => e.Status, s =>
        {
            s.Property(x => x.Value)
                .HasColumnName("Status")
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.Comment).HasColumnType("text");
        builder.Property(e => e.AdminReply).HasColumnType("text");
        builder.Property(e => e.RejectionReason).HasMaxLength(500);
        builder.Property(e => e.IsVerifiedPurchase).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.LikeCount).IsRequired();
        builder.Property(e => e.DislikeCount).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);

        builder.ToTable("ProductReviews");
    }
}