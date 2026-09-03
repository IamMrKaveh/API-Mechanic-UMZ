using global::Domain.Review.Aggregates;
using global::Domain.Review.ValueObjects;
using Tests.TestInfrastructure.Base;
using Reviews = global::Domain.Review.Aggregates.ProductReview;

namespace Tests.Infrastructure.Review.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ReviewConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<(global::Domain.User.Aggregates.User user, global::Domain.Product.Aggregates.Product product)> SeedUserAndProductAsync(
        CancellationToken ct = default)
    {
        var user = await SeedUserAsync(ct: ct);
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName($"Review Product {suffix}")
            .WithSlug($"review-product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();

        Context.Products.Add(product);
        await Context.SaveChangesAsync(ct);
        return (user, product);
    }

    private async Task<Reviews> PersistAsync(Reviews review, CancellationToken ct = default)
    {
        review.ClearDomainEvents();
        Context.ProductReviews.Add(review);
        await Context.SaveChangesAsync(ct);
        return review;
    }

    [Fact]
    public async Task SaveChanges_PersistsReviewAndRoundTripsAllScalarProperties()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .WithRating(5)
            .WithTitle("Excellent product")
            .WithComment("Works perfectly for my car.")
            .WithVerifiedPurchase(true)
            .Build();
        await PersistAsync(review);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.ProductReviews.FirstOrDefaultAsync(r => r.Id == review.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(review.Id);
        reloaded.UserId.ShouldBe(user.Id);
        reloaded.ProductId.ShouldBe(product.Id);
        reloaded.OrderId.ShouldBeNull();
        reloaded.Rating.Value.ShouldBe(5);
        reloaded.Title.ShouldBe("Excellent product");
        reloaded.Comment.ShouldBe("Works perfectly for my car.");
        reloaded.Status.Value.ShouldBe("Pending");
        reloaded.IsVerifiedPurchase.ShouldBeTrue();
        reloaded.IsDeleted.ShouldBeFalse();
        reloaded.LikeCount.ShouldBe(0);
        reloaded.DislikeCount.ShouldBe(0);
        reloaded.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_ApproveRejectAndAdminReply_PersistStateTransitions()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        await PersistAsync(review);

        review.Approve();
        review.AddAdminReply("Thanks for your feedback.");
        review.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.ProductReviews.FirstAsync(r => r.Id == review.Id);

        reloaded.Status.Value.ShouldBe("Approved");
        reloaded.AdminReply.ShouldBe("Thanks for your feedback.");
        reloaded.RepliedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveChanges_WhenReviewIsDeleted_VotesAreCascadeDeleted()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var voter = await SeedUserAsync();
        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .BuildApproved();
        review.AddLike(voter.Id);
        await PersistAsync(review);
        Context.ChangeTracker.Clear();

        var voteCount = await Context.ReviewVotes.CountAsync(v => v.ReviewId == review.Id);
        voteCount.ShouldBe(1);

        var loaded = await Context.ProductReviews
            .Include(r => r.Votes)
            .FirstAsync(r => r.Id == review.Id);
        Context.ProductReviews.Remove(loaded);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var remaining = await Context.ReviewVotes.CountAsync(v => v.ReviewId == review.Id);
        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task SaveChanges_WhenUserHasReviews_DeletingUserIsRestricted()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        await PersistAsync(review);
        Context.ChangeTracker.Clear();

        var loadedUser = await Context.Users.FirstAsync(u => u.Id == user.Id);
        Context.Users.Remove(loadedUser);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenProductHasReviews_DeletingProductIsRestricted()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        await PersistAsync(review);
        Context.ChangeTracker.Clear();

        var loadedProduct = await Context.Products.FirstAsync(p => p.Id == product.Id);
        Context.Products.Remove(loadedProduct);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_SoftDeletedReview_RemainsQueryableWithoutGlobalFilter()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        await PersistAsync(review);

        review.MarkAsDeleted();
        review.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.ProductReviews.FirstOrDefaultAsync(r => r.Id == review.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void Model_MappedToProductReviewsTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("ProductReviews");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Reviews.Id));
    }

    [Fact]
    public void Model_Title_HasMaxLength100()
    {
        var property = Context.Model.FindEntityType(typeof(Reviews))!.FindProperty(nameof(Reviews.Title));
        property.ShouldNotBeNull();
        property!.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_RejectionReason_HasMaxLength500()
    {
        var property = Context.Model.FindEntityType(typeof(Reviews))!.FindProperty(nameof(Reviews.RejectionReason));
        property.ShouldNotBeNull();
        property!.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void Model_CommentAndAdminReply_AreTextColumns()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(Reviews.Comment))!.GetColumnType().ShouldBe("text");
        entityType.FindProperty(nameof(Reviews.AdminReply))!.GetColumnType().ShouldBe("text");
    }

    [Fact]
    public void Model_Status_IsRequiredWithMaxLength50AndNamedIndex()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        var status = entityType!.FindProperty(nameof(Reviews.Status));
        status.ShouldNotBeNull();
        status!.IsNullable.ShouldBeFalse();
        status.GetMaxLength().ShouldBe(50);
        status.GetValueConverter().ShouldNotBeNull();

        var index = entityType.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Reviews.Status));
        index.ShouldNotBeNull();
        index!.GetDatabaseName().ShouldBe("IX_ProductReviews_Status");
    }

    [Fact]
    public void Model_Rating_IsRequiredOwnedValue()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Reviews.Rating));
        navigation.ShouldNotBeNull();
        navigation!.IsCollection.ShouldBeFalse();
    }

    [Fact]
    public void Model_UserAndProduct_ForeignKeysAreRequiredWithRestrictDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        var userFk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Reviews.UserId));
        userFk.ShouldNotBeNull();
        userFk!.IsRequired.ShouldBeTrue();
        userFk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);

        var productFk = entityType.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Reviews.ProductId));
        productFk.ShouldNotBeNull();
        productFk!.IsRequired.ShouldBeTrue();
        productFk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_Order_ForeignKeyIsOptionalWithSetNullDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        var orderFk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Reviews.OrderId));
        orderFk.ShouldNotBeNull();
        orderFk!.IsRequired.ShouldBeFalse();
        orderFk.DeleteBehavior.ShouldBe(DeleteBehavior.SetNull);
    }

    [Fact]
    public void Model_HasCascadeDeleteFromReviewToVotes()
    {
        var navigation = Context.Model.FindEntityType(typeof(Reviews))!.FindNavigation(nameof(Reviews.Votes));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_HasUniqueFilteredIndexOnUserProductOrder()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_ProductReviews_UserId_ProductId_OrderId_Unique");
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
        index.Properties.Select(p => p.Name).ShouldBe(
            [nameof(Reviews.UserId), nameof(Reviews.ProductId), nameof(Reviews.OrderId)],
            ignoreOrder: true);
        index.GetFilter().ShouldContain("IsDeleted");
    }

    [Fact]
    public void Model_HasExpectedSingleColumnIndexes()
    {
        var entityType = Context.Model.FindEntityType(typeof(Reviews));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(Reviews.ProductId), nameof(Reviews.UserId), nameof(Reviews.CreatedAt) })
        {
            var index = entityType!.GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
            index.ShouldNotBeNull($"index on {propertyName} should exist");
        }
    }

    [Fact]
    public void Model_HasXminConcurrencyToken()
    {
        var xmin = Context.Model.FindEntityType(typeof(Reviews))!.FindProperty("xmin");
        xmin.ShouldNotBeNull();
        xmin!.IsConcurrencyToken.ShouldBeTrue();
    }
}
