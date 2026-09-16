[بازگشت به فهرست ماژول‌ها](README.md)

# نظرها و امتیازها (Reviews)

`ProductReview` نظر کاربران روی محصولات را با امتیاز ۱ تا ۵، وضعیت تأیید، پاسخ ادمین و رأی
موافق/مخالف نگه می‌دارد و آمار امتیاز محصول را به‌روز می‌کند. Application آن ۱۷ Command و ۷ Query دارد و
۲۵ اکشن در دو کنترلر (عمومی/کاربر و مدیریتی) ارائه می‌کند. سقف نرخ این ماژول در
[05-api/conventions.md](../05-api/conventions.md) فهرست شده و آمار امتیاز محصول در [products.md](products.md) است.

## دامنه (`Domain/Review`)

`ProductReview : AggregateRoot<ReviewId>, IAuditable` با `ProductId`, `UserId`, `OrderId?`, `Rating`,
`Title?`, `Comment?`, `Status`, `IsVerifiedPurchase`, `LikeCount`, `DislikeCount`, `AdminReply?`,
`RepliedAt?`, `RejectionReason?`, `IsDeleted` و مجموعه رأی‌ها.

| متد | قاعده/اثر |
|---|---|
| `Create` | عنوان حداکثر ۱۰۰ و متن حداکثر ۱۰۰۰ کاراکتر؛ وضعیت آغازین `Pending`؛ صدور `ReviewSubmittedEvent` |
| `UpdateContent` | نظر تأییدشده قابل ویرایش نیست؛ بازگشت وضعیت به `Pending` و پاک‌شدن دلیل رد؛ صدور `ReviewContentUpdatedEvent` |
| `Approve` | idempotent و پاک‌کننده دلیل رد؛ صدور `ReviewApprovedEvent` |
| `Reject(reason)` | دلیل الزامی و حداکثر ۵۰۰ کاراکتر؛ صدور `ReviewRejectedEvent` |
| `AddAdminReply` | پاسخ الزامی و حداکثر ۱۰۰۰ کاراکتر؛ اگر وضعیت `Pending` باشد خودکار تأیید می‌کند؛ صدور `ReviewAdminRepliedEvent` |
| `UpdateAdminReply` / `RemoveAdminReply` | ویرایش مستلزم وجود پاسخ و حذف idempotent |
| `MarkAsDeleted` / `Restore` | حذف نرم و بازگردانی idempotent |
| `AddLike` / `AddDislike` / `RemoveVote` | رأی‌گیری فقط روی نظر تأییدشده و غیرحذف‌شده؛ رأی به نظر خود ممنوع؛ تغییر رأی موجود پشتیبانی می‌شود؛ صدور `ReviewVoteChangedEvent` |

ValueObjectها: `Rating` (۱ تا ۵؛ خطای «امتیاز باید بین ۱ تا ۵ باشد.»)، `ReviewStatus` با مقادیر
`Pending` («در انتظار تأیید»)، `Approved` («تأیید شده») و `Rejected` («رد شده»)، `ReviewId`, `ReviewVoteId`.
Entity `ReviewVote : Entity<ReviewVoteId>` با `VoteType { Like = 1, Dislike = 2 }` و متدهای داخلی
`Create`/`ChangeType`.

پوشه `Exceptions/` خالی است؛ خطاها با `DomainException` و پیام فارسی برگردانده می‌شوند.

Domain Eventها (۸): `ReviewSubmittedEvent`, `ReviewApprovedEvent`, `ReviewRejectedEvent`,
`ReviewContentUpdatedEvent`, `ReviewDeletedEvent`, `ReviewRestoredEvent`, `ReviewAdminRepliedEvent`,
`ReviewVoteChangedEvent`.

### بررسی خرید (اختیاری)

`ReviewDomainService.SubmitReviewAsync(..., requirePurchaseVerification, ...)` دو قاعده دارد: نظر تکراری
(`Review.AlreadyExists`) و «برای ثبت نظر باید محصول را خریداری کرده باشید.» (`Review.NotPurchased`).
تنظیمات در `Application/Review/Configuration/ReviewSettings.cs` (بخش `ReviewSettings`):
`RequirePurchaseVerification` **پیش‌فرض false** و `PurchaseReviewWindowDays` پیش‌فرض ۹۰؛ همچنین
`MaxCommentLength=1000`, `MinCommentLength=10`, `MaxTitleLength=100`, `MaxRejectionReasonLength=500`,
`MaxAdminReplyLength=1000` و `EnableLikeDislike` **پیش‌فرض false**.
پیاده‌سازی `Infrastructure/Review/Services/PurchaseVerificationService.cs` خرید را معتبر می‌داند اگر
آیتم سفارش مطابق باشد، سفارش به کاربر تعلق داشته باشد، وضعیت سفارش `Delivered` باشد و تحویل در
N روز گذشته (پیش‌فرض ۹۰) رخ داده باشد.

## Application (`Application/Review`)

**۱۷ Command:** `CreateReview`, `UpdateOwnReview`, `DeleteOwnReview`, `ApproveReview`, `RejectReview`,
`UpdateReviewStatus`, `DeleteReview`, `RestoreReview`, `ReplyToReview`, `UpdateAdminReply`,
`RemoveAdminReply`, `LikeReview`, `DislikeReview`, `RemoveReviewVote` و سه Command گروهی در پوشه
`BulkOperation`: `BulkApproveReviews`, `BulkRejectReviews`, `BulkDeleteReviews` (سقف ۱ تا ۱۰۰ شناسه در هر
درخواست گروهی).

**۷ Query:** `GetProductReviews`, `GetProductReviewSummary`, `GetReviewById`, `GetUserReviews`,
`GetReviewsByStatus`, `CanReviewProduct`, `GetAdminReviewStats`.

**۵ Event Handler** (همه آمار محصول را بازمحاسبه و ذخیره می‌کنند):
`UpdateProductStatsOnReviewApprovedHandler`, `UpdateProductStatsOnReviewContentUpdatedHandler`,
`UpdateProductStatsOnReviewDeletedHandler`, `UpdateProductStatsOnReviewRejectedHandler`,
`UpdateProductStatsOnReviewRestoredHandler`. برای `ReviewSubmittedEvent`, `ReviewVoteChangedEvent` و
`ReviewAdminRepliedEvent` Handlerی وجود ندارد.

فعال‌بودن رأی‌گیری از فلگ `ReviewSettings.EnableLikeDislike` پیروی می‌کند و در سه Handler
`LikeReview`, `DislikeReview` و `RemoveReviewVote` بررسی می‌شود.

## API

جمع: **۲ کنترلر و ۲۵ اکشن**. این تنها ماژولی است که فیلتر سقف نرخ اختصاصی دارد؛ سیاست‌ها
(`CreateReview`, `PublicRead`, `AdminAction`, `Vote`) با پنجره یک‌دقیقه‌ای و محدودیت‌های پیش‌فرض
(۵، ۶۰، ۳۰ و ۲۰ در دقیقه) در `ReviewSettings.RateLimit` تنظیم می‌شوند و پاسخ عبور از سقف، ۴۲۹ با هدر
`Retry-After` است.

`ReviewsController` — `[Route("api/v{version:apiVersion}/reviews")]` — بدون اتریبیوت کلاس؛ ۱۱ اکشن:

| متد | مسیر | اکشن | دسترسی | سقف نرخ |
|---|---|---|---|---|
| GET | `products/{productId:guid}` | `GetReviews` | `[AllowAnonymous]` | `PublicRead` |
| GET | `products/{productId:guid}/summary` | `GetSummary` | `[AllowAnonymous]` | `PublicRead` |
| GET | `products/{productId:guid}/can-review` | `CanReview` | `[Authorize]` | — |
| POST | — | `CreateReview` | `[Authorize]` | `CreateReview` |
| GET | `{reviewId:guid}` | `GetById` | `[AllowAnonymous]` | — |
| PUT | `{reviewId:guid}` | `UpdateOwn` | `[Authorize]` | `CreateReview` |
| DELETE | `me/{reviewId:guid}` | `DeleteOwn` | `[Authorize]` | — |
| GET | `me` | `GetMyReviews` | `[Authorize]` | — |
| POST | `{reviewId:guid}/like` | `LikeReview` | `[Authorize]` | `Vote` |
| POST | `{reviewId:guid}/dislike` | `DislikeReview` | `[Authorize]` | `Vote` |
| DELETE | `{reviewId:guid}/vote` | `RemoveReviewVote` | `[Authorize]` | `Vote` |

`AdminReviewsController` — `[Route("api/v{version:apiVersion}/admin/reviews")]` — `[Authorize(Roles = "Admin")]`؛ ۱۴ اکشن:
`GET` → `GetReviewsByStatus`، `GET stats` → `GetStats`، `GET {reviewId:guid}` → `GetById`،
`PATCH {reviewId:guid}/approve` → `ApproveReview`، `PATCH {reviewId:guid}/reject` → `RejectReview`،
`PATCH {reviewId:guid}/status` → `UpdateReviewStatus`، `DELETE {reviewId:guid}` → `DeleteReview`،
`POST {reviewId:guid}/restore` → `RestoreReview`، `POST {reviewId:guid}/reply` → `ReplyToReview`،
`PUT {reviewId:guid}/reply` → `UpdateReply`، `DELETE {reviewId:guid}/reply` → `RemoveReply`،
`POST bulk/approve` → `BulkApprove`، `POST bulk/reject` → `BulkReject`، `POST bulk/delete` → `BulkDelete`.
همه اکشن‌های ادمین (به‌جز `GetById`) سیاست `AdminAction` دارند.

### چرخه تعدیل

ثبت → `Pending` → تأیید/رد توسط ادمین (یا تغییر وضعیت مستقیم) → ویرایش توسط صاحب نظر، وضعیت را به
`Pending` برمی‌گرداند → پاسخ ادمین روی نظر `Pending` آن را خودکار تأیید می‌کند → حذف نرم و بازگردانی.

## زیرساخت

- نگاشت EF در `ReviewConfiguration` و `ReviewVoteConfiguration`؛ `ReviewRepository` نوشتن و
  `ReviewQueryService` خواندن را تأمین می‌کند. **کشی در این ماژول وجود ندارد.**
- `ReviewConfigurationExtensions.AddReviewSettings` بخش `"ReviewSettings"` را با `ValidateOnStart`
  اعتبارسنجی می‌کند؛ یعنی مقدار نامعتبر مانع استارت برنامه می‌شود.
- فیلتر `ReviewRateLimitFilter` و اتریبیوت `[ReviewRateLimit]` در `Presentation/Common/Filters/` هستند و
  به‌صورت سراسری ثبت می‌شوند؛ کلیدها ترکیب شناسه کاربر یا IP با نام سیاست‌اند.

## نکات و محدودیت‌ها

- رأی‌گیری موافق/مخالف به‌صورت پیش‌فرض **خاموش** است (`EnableLikeDislike=false`)؛ سه Endpoint رأی در
  تنظیمات پیش‌فرض بی‌اثرند.
- الزام «خرید تأییدشده» به‌صورت پیش‌فرض خاموش است؛ حتی در حالت روشن، خرید فقط با وضعیت `Delivered`
  و در پنجره ۹۰ روزه معتبر شمرده می‌شود.
- آمار امتیاز محصول فقط برای رویدادهای تأیید/ویرایش/حذف/رد/بازگردانی بازمحاسبه می‌شود؛ نظرهای
  `Pending` و رأی‌ها وارد میانگین نمی‌شوند.
- ویرایش نظر تأییدشده ممنوع است؛ مسیر درست برای کاربر، ثبت مجدد است چون `UpdateOwnReview` روی نظر
  `Approved` خطای دامنه می‌دهد.

## اسناد مرتبط

- محصول و آمار امتیاز: [products.md](products.md)
- پروفایل کاربر و نظرهای من: [identity/users.md](identity/users.md)
- اعلان و تیکت: [notifications.md](notifications.md)، [support.md](support.md)
- فیلترها و سقف نرخ سراسری: [05-api/conventions.md](../05-api/conventions.md)
