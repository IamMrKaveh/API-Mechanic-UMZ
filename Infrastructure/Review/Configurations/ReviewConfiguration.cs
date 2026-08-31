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
        builder.ToTable("ProductReviews");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => ReviewId.From(v));

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(e => e.ProductId)
            .HasConversion(v => v.Value, v => ProductId.From(v))
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .IsRequired();

        var orderIdConverter = new ValueConverter<OrderId?, Guid?>(
            v => v == null ? null : v.Value,
            v => v.HasValue ? OrderId.From(v.Value) : null);

        builder.Property(e => e.OrderId)
            .HasConversion(orderIdConverter)
            .IsRequired(false);

        builder.OwnsOne(e => e.Rating, r =>
        {
            r.WithOwner();

            r.Property(x => x.Value)
                .HasColumnName("Rating")
                .IsRequired();
        });

        builder.Navigation(e => e.Rating).IsRequired();

        var reviewStatusConverter = new ValueConverter<ReviewStatus, string>(
            v => v.Value,
            v => ReviewStatus.From(v));

        var reviewStatusComparer = new ValueComparer<ReviewStatus>(
            (l, r) => (l == null && r == null) || (l != null && r != null && l.Value == r.Value),
            v => v == null ? 0 : v.Value.GetHashCode(),
            v => v == null ? null! : ReviewStatus.From(v.Value));

        builder.Property(e => e.Status)
            .HasConversion(reviewStatusConverter)
            .Metadata.SetValueComparer(reviewStatusComparer);

        builder.Property(e => e.Status)
            .HasColumnName("Status")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => e.Status).HasDatabaseName("IX_ProductReviews_Status");

        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.Comment).HasColumnType("text");
        builder.Property(e => e.AdminReply).HasColumnType("text");
        builder.Property(e => e.RejectionReason).HasMaxLength(500);
        builder.Property(e => e.IsVerifiedPurchase).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.LikeCount).IsRequired();
        builder.Property(e => e.DislikeCount).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasMany(e => e.Votes)
            .WithOne()
            .HasForeignKey("ReviewId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(ProductReview.Votes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);

        builder.HasIndex(e => new { e.UserId, e.ProductId, e.OrderId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_ProductReviews_UserId_ProductId_OrderId_Unique");
    }
}
