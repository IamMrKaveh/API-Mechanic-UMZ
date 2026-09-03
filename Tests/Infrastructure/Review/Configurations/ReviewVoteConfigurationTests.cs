using global::Domain.Review.Entities;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Review.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ReviewVoteConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<global::Domain.Review.Aggregates.ProductReview> SeedApprovedReviewAsync(
        global::Domain.User.ValueObjects.UserId? voterExclusion = null,
        CancellationToken ct = default)
    {
        var author = await SeedUserAsync(ct: ct);
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName($"Vote Product {suffix}")
            .WithSlug($"vote-product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();

        Context.Products.Add(product);
        await Context.SaveChangesAsync(ct);

        var review = new ProductReviewBuilder()
            .WithUserId(author.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .BuildApproved();
        review.ClearDomainEvents();

        Context.ProductReviews.Add(review);
        await Context.SaveChangesAsync(ct);
        return review;
    }

    [Fact]
    public async Task SaveChanges_LikeVote_PersistsAndRoundTrips()
    {
        var review = await SeedApprovedReviewAsync();
        var voter = await SeedUserAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.ProductReviews
            .Include(r => r.Votes)
            .FirstAsync(r => r.Id == review.Id);
        loaded.AddLike(voter.Id);
        loaded.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var vote = await Context.ReviewVotes.FirstOrDefaultAsync(v => v.ReviewId == review.Id && v.UserId == voter.Id);

        vote.ShouldNotBeNull();
        vote!.ReviewId.ShouldBe(review.Id);
        vote.UserId.ShouldBe(voter.Id);
        vote.Type.ShouldBe(global::Domain.Review.Enums.VoteType.Like);
        vote.CreatedAt.ShouldNotBe(default);
        vote.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_ChangeVoteType_UpdatesTypeAndSetsUpdatedAt()
    {
        var review = await SeedApprovedReviewAsync();
        var voter = await SeedUserAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.ProductReviews
            .Include(r => r.Votes)
            .FirstAsync(r => r.Id == review.Id);
        loaded.AddLike(voter.Id);
        loaded.ClearDomainEvents();
        await Context.SaveChangesAsync();

        loaded.AddDislike(voter.Id);
        loaded.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var votes = await Context.ReviewVotes.Where(v => v.ReviewId == review.Id && v.UserId == voter.Id).ToListAsync();

        votes.Count.ShouldBe(1);
        votes[0].Type.ShouldBe(global::Domain.Review.Enums.VoteType.Dislike);
        votes[0].UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveChanges_DuplicateVoteForSameUser_DoesNotCreateSecondRow()
    {
        var review = await SeedApprovedReviewAsync();
        var voter = await SeedUserAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.ProductReviews
            .Include(r => r.Votes)
            .FirstAsync(r => r.Id == review.Id);
        loaded.AddLike(voter.Id);
        loaded.AddLike(voter.Id);
        loaded.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var count = await Context.ReviewVotes.CountAsync(v => v.ReviewId == review.Id && v.UserId == voter.Id);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_DuplicateReviewIdUserId_InsertedViaSql_ThrowsDbUpdateException()
    {
        var review = await SeedApprovedReviewAsync();
        var voter = await SeedUserAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.ProductReviews
            .Include(r => r.Votes)
            .FirstAsync(r => r.Id == review.Id);
        loaded.AddLike(voter.Id);
        loaded.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var entityType = Context.Model.FindEntityType(typeof(ReviewVote))!;
        var table = entityType.GetTableName();
        var columns = entityType.GetProperties().ToDictionary(p => p.Name, p => p.GetColumnName());

        var sql = $"INSERT INTO \"{table}\" (\"{columns["Id"]}\", \"{columns["ReviewId"]}\", \"{columns["UserId"]}\", \"{columns["Type"]}\", \"{columns["CreatedAt"]}\") " +
                  $"VALUES ('{Guid.NewGuid()}', '{review.Id.Value}', '{voter.Id.Value}', 'Like', NOW())";

        await Should.ThrowAsync<Exception>(() => Context.Database.ExecuteSqlRawAsync(sql));
        Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SaveChanges_WhenReviewIsDeleted_VotesAreCascadeDeleted()
    {
        var review = await SeedApprovedReviewAsync();
        var voter = await SeedUserAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.ProductReviews
            .Include(r => r.Votes)
            .FirstAsync(r => r.Id == review.Id);
        loaded.AddLike(voter.Id);
        loaded.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var withVotes = await Context.ProductReviews
            .Include(r => r.Votes)
            .FirstAsync(r => r.Id == review.Id);
        Context.ProductReviews.Remove(withVotes);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var remaining = await Context.ReviewVotes.CountAsync(v => v.ReviewId == review.Id);
        remaining.ShouldBe(0);
    }

    [Fact]
    public void Model_MappedToReviewVotesTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(ReviewVote));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("ReviewVotes");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(ReviewVote));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(ReviewVote.Id));
    }

    [Fact]
    public void Model_ReviewIdAndUserId_AreRequiredWithConverters()
    {
        var entityType = Context.Model.FindEntityType(typeof(ReviewVote));
        entityType.ShouldNotBeNull();

        var reviewId = entityType!.FindProperty(nameof(ReviewVote.ReviewId));
        reviewId.ShouldNotBeNull();
        reviewId!.IsNullable.ShouldBeFalse();
        reviewId.GetValueConverter().ShouldNotBeNull();

        var userId = entityType.FindProperty(nameof(ReviewVote.UserId));
        userId.ShouldNotBeNull();
        userId!.IsNullable.ShouldBeFalse();
        userId.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_Type_IsRequiredStringWithMaxLength20()
    {
        var property = Context.Model.FindEntityType(typeof(ReviewVote))!.FindProperty(nameof(ReviewVote.Type));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(20);
    }

    [Fact]
    public void Model_CreatedAtIsRequired_UpdatedAtIsOptional()
    {
        var entityType = Context.Model.FindEntityType(typeof(ReviewVote));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(ReviewVote.CreatedAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(ReviewVote.UpdatedAt))!.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Model_HasUniqueIndexOnReviewIdUserIdWithExpectedName()
    {
        var entityType = Context.Model.FindEntityType(typeof(ReviewVote));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_ReviewVotes_ReviewId_UserId_Unique");
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
        index.Properties.Select(p => p.Name).ShouldBe(
            [nameof(ReviewVote.ReviewId), nameof(ReviewVote.UserId)],
            ignoreOrder: true);
    }

    [Fact]
    public void Model_HasNonUniqueIndexOnReviewId()
    {
        var entityType = Context.Model.FindEntityType(typeof(ReviewVote));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(ReviewVote.ReviewId) && !i.IsUnique);
        index.ShouldNotBeNull();
    }
}
