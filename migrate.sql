CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "AttributeTypes" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "DisplayName" character varying(100) NOT NULL,
    "SortOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_AttributeTypes" PRIMARY KEY ("Id")
);

CREATE TABLE "Categories" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Slug" character varying(200) NOT NULL,
    "Description" character varying(1000),
    "IsActive" boolean NOT NULL,
    "SortOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Id")
);

CREATE TABLE "DiscountCodes" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "DiscountValue" numeric(18,4) NOT NULL,
    "DiscountType" character varying(50) NOT NULL,
    "MaximumDiscountAmount" numeric(18,4),
    "UsageLimit" integer,
    "UsageCount" integer NOT NULL,
    "StartsAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_DiscountCodes" PRIMARY KEY ("Id")
);

CREATE TABLE "ElasticsearchOutboxMessages" (
    "Id" uuid NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "Document" text NOT NULL,
    "ChangeType" character varying(50) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "RetryCount" integer NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "Error" character varying(2000),
    CONSTRAINT "PK_ElasticsearchOutboxMessages" PRIMARY KEY ("Id")
);

CREATE TABLE "FailedElasticOperations" (
    "Id" uuid NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" character varying(200) NOT NULL,
    "Document" text NOT NULL,
    "Error" text NOT NULL,
    "Status" character varying(50) NOT NULL,
    "RetryCount" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "LastRetryAt" timestamp with time zone,
    CONSTRAINT "PK_FailedElasticOperations" PRIMARY KEY ("Id")
);

CREATE TABLE "Medias" (
    "Id" uuid NOT NULL,
    "FilePath" character varying(1000) NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "FileExtension" character varying(50) NOT NULL,
    "FileSize" bigint NOT NULL,
    "FileType" character varying(100) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "SortOrder" integer NOT NULL,
    "IsPrimary" boolean NOT NULL,
    "AltText" character varying(500),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Medias" PRIMARY KEY ("Id")
);

CREATE TABLE "OrderProcessStates" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "CurrentStep" character varying(50) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "FailureReason" character varying(500),
    "RetryCount" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CorrelationId" character varying(200),
    CONSTRAINT "PK_OrderProcessStates" PRIMARY KEY ("Id")
);

CREATE TABLE "OrderStatuses" (
    "Id" uuid NOT NULL,
    "Name" character varying(50) NOT NULL,
    "DisplayName" character varying(100) NOT NULL,
    "Icon" character varying(100),
    "Color" character varying(50),
    "SortOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "IsDefault" boolean NOT NULL,
    "AllowCancel" boolean NOT NULL,
    "AllowEdit" boolean NOT NULL,
    CONSTRAINT "PK_OrderStatuses" PRIMARY KEY ("Id")
);

CREATE TABLE "OutboxMessages" (
    "Id" uuid NOT NULL,
    "Type" character varying(500) NOT NULL,
    "Payload" text NOT NULL,
    created_at timestamp with time zone NOT NULL,
    processed_at timestamp with time zone,
    "Error" text,
    retry_count integer NOT NULL,
    CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
);

CREATE TABLE "RateLimitEntries" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Key" character varying(300) NOT NULL,
    "WindowKey" character varying(100) NOT NULL,
    "Count" integer NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_RateLimitEntries" PRIMARY KEY ("Id")
);

CREATE TABLE "Shippings" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(500),
    "Cost" numeric(18,2) NOT NULL,
    "CostCurrency" character varying(10) NOT NULL,
    "EstimatedDeliveryTime" character varying(200),
    "MinDeliveryDays" integer NOT NULL,
    "MaxDeliveryDays" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "SortOrder" integer NOT NULL,
    "IsDefault" boolean NOT NULL,
    "MinOrderAmount" numeric(18,2),
    "MinOrderAmountCurrency" character varying(10),
    "MaxOrderAmount" numeric(18,2),
    "MaxOrderAmountCurrency" character varying(10),
    "MaxWeight" numeric(18,2),
    "FreeShippingEnabled" boolean NOT NULL,
    "FreeShippingThreshold" numeric(18,2),
    "FreeShippingThresholdCurrency" character varying(10),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Shippings" PRIMARY KEY ("Id")
);

CREATE TABLE "UserOtps" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CodeHash" character varying(128) NOT NULL,
    "Purpose" character varying(50) NOT NULL,
    "IsVerified" boolean NOT NULL,
    "VerificationAttempts" integer NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "VerifiedAt" timestamp with time zone,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_UserOtps" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PhoneNumber" character varying(20),
    "PasswordHash" character varying(500),
    "IsActive" boolean NOT NULL,
    "IsAdmin" boolean NOT NULL,
    "IsEmailVerified" boolean NOT NULL,
    "FailedLoginAttempts" integer NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "LastLoginAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DefaultAddressId" uuid,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "UserSessions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RefreshToken" character varying(512) NOT NULL,
    "DeviceInfo" character varying(500) NOT NULL,
    "IpAddress" character varying(45) NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "RevocationReason" character varying(50),
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone,
    "LastActivityAt" timestamp with time zone,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_UserSessions" PRIMARY KEY ("Id")
);

CREATE TABLE "WalletReconciliationAudit" (
    "Id" uuid NOT NULL,
    "WalletId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "SnapshotBalance" numeric(18,2) NOT NULL,
    "LedgerBalance" numeric(18,2) NOT NULL,
    "Delta" numeric(18,2) NOT NULL,
    "DetectedAt" timestamp with time zone NOT NULL,
    "Notes" text,
    CONSTRAINT "PK_WalletReconciliationAudit" PRIMARY KEY ("Id")
);

CREATE TABLE "Warehouses" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "City" character varying(100) NOT NULL,
    "Address" character varying(500),
    "Phone" character varying(20),
    "IsActive" boolean NOT NULL,
    "IsDefault" boolean NOT NULL,
    "Priority" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Warehouses" PRIMARY KEY ("Id")
);

CREATE TABLE "AttributeValues" (
    "Id" uuid NOT NULL,
    "Value" character varying(100) NOT NULL,
    "DisplayValue" character varying(100) NOT NULL,
    "HexCode" character varying(50),
    "SortOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "AttributeTypeId" uuid NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "RowVersion" bytea,
    CONSTRAINT "PK_AttributeValues" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AttributeValues_AttributeTypes_AttributeTypeId" FOREIGN KEY ("AttributeTypeId") REFERENCES "AttributeTypes" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Brands" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Slug" character varying(200) NOT NULL,
    "Description" character varying(500),
    "LogoPath" character varying(1000),
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CategoryId" uuid NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Brands" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Brands_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DiscountRestrictions" (
    "Id" uuid NOT NULL,
    "DiscountCodeId" uuid NOT NULL,
    "RestrictionType" character varying(50) NOT NULL,
    "RestrictionValue" character varying(500) NOT NULL,
    CONSTRAINT "PK_DiscountRestrictions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DiscountRestrictions_DiscountCodes_DiscountCodeId" FOREIGN KEY ("DiscountCodeId") REFERENCES "DiscountCodes" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "UserId" uuid,
    "EventType" character varying(100) NOT NULL,
    "Action" character varying(200) NOT NULL,
    "Details" text,
    "IpAddress" character varying(45) NOT NULL,
    "UserAgent" character varying(500),
    "EntityType" character varying(100),
    "EntityId" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL,
    "IntegrityHash" character varying(200) NOT NULL,
    "IsArchived" boolean NOT NULL,
    "ArchivedAt" timestamp with time zone,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);

CREATE TABLE "Carts" (
    "Id" uuid NOT NULL,
    "GuestToken" character varying(256),
    "IsCheckedOut" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UserId" uuid,
    "AppliedDiscountCodeId" uuid,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Carts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Carts_DiscountCodes_AppliedDiscountCodeId" FOREIGN KEY ("AppliedDiscountCodeId") REFERENCES "DiscountCodes" ("Id"),
    CONSTRAINT "FK_Carts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);

CREATE TABLE "Notifications" (
    "Id" uuid NOT NULL,
    "IsRead" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UserId" uuid NOT NULL,
    "ActionUrl" character varying(500),
    "Message" character varying(1000) NOT NULL,
    "RelatedEntityId" uuid,
    "RelatedEntityType" character varying(100),
    "Title" character varying(200) NOT NULL,
    "Type" character varying(100) NOT NULL,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Orders" (
    "Id" uuid NOT NULL,
    "OrderNumber" character varying(50) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "ReceiverFullName" character varying(150) NOT NULL,
    "ReceiverPhoneNumber" character varying(20) NOT NULL,
    "DeliveryProvince" character varying(100) NOT NULL,
    "DeliveryCity" character varying(100) NOT NULL,
    "DeliveryStreet" character varying(300) NOT NULL,
    "DeliveryPostalCode" character varying(20) NOT NULL,
    "SubTotalAmount" numeric(18,2) NOT NULL,
    "SubTotalCurrency" character varying(5) NOT NULL,
    "ShippingCostAmount" numeric(18,2) NOT NULL,
    "ShippingCostCurrency" character varying(5) NOT NULL,
    "DiscountAmount" numeric(18,2) NOT NULL,
    "DiscountCurrency" character varying(5) NOT NULL,
    "FinalAmount" numeric(18,2) NOT NULL,
    "FinalCurrency" character varying(5) NOT NULL,
    "IdempotencyKey" uuid NOT NULL,
    "CancellationReason" character varying(500),
    "IsDeleted" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UserId" uuid NOT NULL,
    "AppliedDiscountCodeId" uuid,
    "PaymentTransactionId" uuid,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Orders_DiscountCodes_AppliedDiscountCodeId" FOREIGN KEY ("AppliedDiscountCodeId") REFERENCES "DiscountCodes" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Orders_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Tickets" (
    "Id" uuid NOT NULL,
    "UserId" uuid,
    "CustomerId" uuid NOT NULL,
    "AssignedAgentId" uuid,
    "Subject" character varying(500) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "Priority" character varying(50) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "LastActivityAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Tickets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Tickets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);

CREATE TABLE "UserAddresses" (
    "Id" uuid NOT NULL,
    "UserId1" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Title" character varying(100) NOT NULL,
    "ReceiverName" character varying(100) NOT NULL,
    "ReceiverPhoneNumber" character varying(20) NOT NULL,
    "Province" character varying(100) NOT NULL,
    "City" character varying(100) NOT NULL,
    "Address" character varying(500) NOT NULL,
    "PostalCode" character varying(20) NOT NULL,
    "Latitude" numeric(9,6),
    "Longitude" numeric(9,6),
    "IsDefault" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_UserAddresses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserAddresses_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserAddresses_Users_UserId1" FOREIGN KEY ("UserId1") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Wallets" (
    "Id" uuid NOT NULL,
    "CurrentBalance" numeric(18,2) NOT NULL,
    "BalanceCurrency" character varying(10) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UserId" uuid NOT NULL,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Wallets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Wallets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Products" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Slug" character varying(200) NOT NULL,
    "Description" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "IsFeatured" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "BrandId" uuid NOT NULL,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Products_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "DiscountUsageRecords" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "DiscountedAmount" numeric(18,4) NOT NULL,
    "UsageCountAtTime" integer NOT NULL,
    "UsedAt" timestamp with time zone NOT NULL,
    "DiscountCodeId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    CONSTRAINT "PK_DiscountUsageRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DiscountUsageRecords_DiscountCodes_DiscountCodeId" FOREIGN KEY ("DiscountCodeId") REFERENCES "DiscountCodes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DiscountUsageRecords_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_DiscountUsageRecords_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "DiscountUsages" (
    "Id" uuid NOT NULL,
    "DiscountCodeId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "Code" text NOT NULL,
    "DiscountedAmount" numeric NOT NULL,
    "UsageCountAtTime" integer NOT NULL,
    "UsedAt" timestamp with time zone NOT NULL,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_DiscountUsages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DiscountUsages_DiscountCodes_DiscountCodeId" FOREIGN KEY ("DiscountCodeId") REFERENCES "DiscountCodes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DiscountUsages_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DiscountUsages_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PaymentTransactions" (
    "Id" uuid NOT NULL,
    "Authority" character varying(100) NOT NULL,
    "Gateway" character varying(50) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "RefId" bigint,
    "Fee" numeric NOT NULL,
    "ErrorMessage" character varying(500),
    "Description" character varying(500),
    "IsVerificationInProgress" boolean NOT NULL,
    "VerifiedAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "OrderId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PaymentTransactions_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PaymentTransactions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "TicketMessages" (
    "Id" uuid NOT NULL,
    "TicketId" uuid NOT NULL,
    "SenderId" uuid NOT NULL,
    "SenderType" character varying(20) NOT NULL,
    "Content" character varying(5000) NOT NULL,
    "IsEdited" boolean NOT NULL,
    "EditedAt" timestamp with time zone,
    "SentAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_TicketMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TicketMessages_Tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES "Tickets" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WalletLedgerEntries" (
    "Id" uuid NOT NULL,
    "WalletId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "AmountDelta" numeric(18,2) NOT NULL,
    "AmountCurrency" character varying(10) NOT NULL,
    "BalanceAfter" numeric(18,2) NOT NULL,
    "BalanceAfterCurrency" character varying(10) NOT NULL,
    "TransactionType" character varying(50) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "ReferenceId" character varying(200) NOT NULL,
    "IdempotencyKey" character varying(200),
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_WalletLedgerEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WalletLedgerEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WalletLedgerEntries_Wallets_WalletId" FOREIGN KEY ("WalletId") REFERENCES "Wallets" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WalletReservations" (
    "Id" uuid NOT NULL,
    "WalletId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "AmountCurrency" character varying(10) NOT NULL,
    "Purpose" character varying(200) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone,
    "ResolvedAt" timestamp with time zone,
    CONSTRAINT "PK_WalletReservations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WalletReservations_Wallets_WalletId" FOREIGN KEY ("WalletId") REFERENCES "Wallets" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProductReviews" (
    "Id" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "OrderId" uuid,
    "Rating" integer NOT NULL,
    "Title" character varying(100),
    "Comment" text,
    "Status" character varying(50) NOT NULL,
    "IsVerifiedPurchase" boolean NOT NULL,
    "LikeCount" integer NOT NULL,
    "DislikeCount" integer NOT NULL,
    "AdminReply" text,
    "RepliedAt" timestamp with time zone,
    "RejectionReason" character varying(500),
    "IsDeleted" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_ProductReviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProductReviews_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id"),
    CONSTRAINT "FK_ProductReviews_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProductReviews_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProductVariants" (
    "Id" uuid NOT NULL,
    "ProductId1" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Sku" character varying(100) NOT NULL,
    "Price" numeric(18,2) NOT NULL,
    "PriceCurrency" character varying(10) NOT NULL,
    "SellingPrice" numeric(18,2) NOT NULL,
    "SellingPriceCurrency" character varying(10) NOT NULL,
    "CompareAtPrice" numeric(18,2),
    "CompareAtPriceCurrency" character varying(10),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedBy" uuid,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_ProductVariants" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProductVariants_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProductVariants_Products_ProductId1" FOREIGN KEY ("ProductId1") REFERENCES "Products" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Wishlists" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Wishlists" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Wishlists_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Wishlists_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CartItems" (
    "Id" uuid NOT NULL,
    "ProductName" character varying(500) NOT NULL,
    "Sku" character varying(100) NOT NULL,
    "SellingPrice" numeric NOT NULL,
    "SellingPriceCurrency" character varying(10) NOT NULL,
    "OriginalPrice" numeric NOT NULL,
    "OriginalPriceCurrency" character varying(10) NOT NULL,
    "Quantity" integer NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL,
    "CartId" uuid NOT NULL,
    "CartId1" uuid NOT NULL,
    "VariantId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "RowVersion" bytea,
    CONSTRAINT "PK_CartItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CartItems_Carts_CartId" FOREIGN KEY ("CartId") REFERENCES "Carts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CartItems_Carts_CartId1" FOREIGN KEY ("CartId1") REFERENCES "Carts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CartItems_ProductVariants_VariantId" FOREIGN KEY ("VariantId") REFERENCES "ProductVariants" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CartItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Inventories" (
    "Id" uuid NOT NULL,
    "StockQuantity" integer NOT NULL,
    "IsUnlimited" boolean NOT NULL,
    "ReservedQuantity" integer NOT NULL,
    "LowStockThreshold" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "VariantId" uuid NOT NULL,
    "RowVersion" bytea,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Inventories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Inventories_ProductVariants_VariantId" FOREIGN KEY ("VariantId") REFERENCES "ProductVariants" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "OrderItems" (
    "Id" uuid NOT NULL,
    "ProductName" character varying(200) NOT NULL,
    "Sku" character varying(100) NOT NULL,
    "UnitPriceAmount" numeric(18,2) NOT NULL,
    "UnitPriceCurrency" character varying(5) NOT NULL,
    "Quantity" integer NOT NULL,
    "OrderId1" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "VariantId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrderItems_Orders_OrderId1" FOREIGN KEY ("OrderId1") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrderItems_ProductVariants_VariantId" FOREIGN KEY ("VariantId") REFERENCES "ProductVariants" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrderItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProductVariantAttributes" (
    "Id" uuid NOT NULL,
    "VariantId" uuid NOT NULL,
    "AttributeTypeId" uuid NOT NULL,
    "ValueId" uuid NOT NULL,
    "DisplayValue" text NOT NULL,
    "AttributeValueId" uuid,
    CONSTRAINT "PK_ProductVariantAttributes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProductVariantAttributes_AttributeTypes_AttributeTypeId" FOREIGN KEY ("AttributeTypeId") REFERENCES "AttributeTypes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ProductVariantAttributes_AttributeValues_AttributeValueId" FOREIGN KEY ("AttributeValueId") REFERENCES "AttributeValues" ("Id"),
    CONSTRAINT "FK_ProductVariantAttributes_AttributeValues_ValueId" FOREIGN KEY ("ValueId") REFERENCES "AttributeValues" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ProductVariantAttributes_ProductVariants_VariantId" FOREIGN KEY ("VariantId") REFERENCES "ProductVariants" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProductVariantShippings" (
    "Id" uuid NOT NULL,
    "VariantId" uuid NOT NULL,
    "ShippingId" uuid NOT NULL,
    "Weight" numeric(10,3) NOT NULL,
    "Width" numeric(10,3) NOT NULL,
    "Height" numeric(10,3) NOT NULL,
    "Length" numeric(10,3) NOT NULL,
    CONSTRAINT "PK_ProductVariantShippings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProductVariantShippings_ProductVariants_VariantId" FOREIGN KEY ("VariantId") REFERENCES "ProductVariants" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProductVariantShippings_Shippings_ShippingId" FOREIGN KEY ("ShippingId") REFERENCES "Shippings" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "StockLedgerEntries" (
    "Id" uuid NOT NULL,
    "VariantId" uuid NOT NULL,
    "WarehouseId" uuid,
    "OrderItemId" uuid,
    "UserId" uuid,
    "EventType" character varying(50) NOT NULL,
    "QuantityDelta" integer NOT NULL,
    "BalanceAfter" integer NOT NULL,
    "UnitCost" numeric(18,4) NOT NULL,
    "ReferenceNumber" character varying(100),
    "CorrelationId" character varying(200),
    "Note" character varying(500),
    "IdempotencyKey" character varying(200) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "InventoryId" uuid,
    CONSTRAINT "PK_StockLedgerEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_StockLedgerEntries_Inventories_InventoryId" FOREIGN KEY ("InventoryId") REFERENCES "Inventories" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StockLedgerEntries_OrderItems_OrderItemId" FOREIGN KEY ("OrderItemId") REFERENCES "OrderItems" ("Id"),
    CONSTRAINT "FK_StockLedgerEntries_ProductVariants_VariantId" FOREIGN KEY ("VariantId") REFERENCES "ProductVariants" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StockLedgerEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_StockLedgerEntries_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_AttributeValues_AttributeTypeId" ON "AttributeValues" ("AttributeTypeId");

CREATE INDEX "IX_AuditLogs_CreatedAt" ON "AuditLogs" ("CreatedAt");

CREATE INDEX "IX_AuditLogs_EventType" ON "AuditLogs" ("EventType");

CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");

CREATE INDEX "IX_Brands_CategoryId_Name" ON "Brands" ("CategoryId", "Name");

CREATE UNIQUE INDEX "IX_Brands_Slug" ON "Brands" ("Slug");

CREATE UNIQUE INDEX "IX_CartItems_CartId_VariantId" ON "CartItems" ("CartId", "VariantId");

CREATE INDEX "IX_CartItems_CartId1" ON "CartItems" ("CartId1");

CREATE INDEX "IX_CartItems_ProductId" ON "CartItems" ("ProductId");

CREATE INDEX "IX_CartItems_VariantId" ON "CartItems" ("VariantId");

CREATE INDEX "IX_Carts_AppliedDiscountCodeId" ON "Carts" ("AppliedDiscountCodeId");

CREATE INDEX "IX_Carts_UserId" ON "Carts" ("UserId");

CREATE UNIQUE INDEX "IX_Categories_Name" ON "Categories" ("Name");

CREATE UNIQUE INDEX "IX_Categories_Slug" ON "Categories" ("Slug");

CREATE UNIQUE INDEX "IX_DiscountCodes_Code" ON "DiscountCodes" ("Code");

CREATE INDEX "IX_DiscountRestrictions_DiscountCodeId" ON "DiscountRestrictions" ("DiscountCodeId");

CREATE INDEX "IX_DiscountUsageRecords_DiscountCodeId" ON "DiscountUsageRecords" ("DiscountCodeId");

CREATE INDEX "IX_DiscountUsageRecords_OrderId" ON "DiscountUsageRecords" ("OrderId");

CREATE INDEX "IX_DiscountUsageRecords_UserId" ON "DiscountUsageRecords" ("UserId");

CREATE INDEX "IX_DiscountUsages_DiscountCodeId" ON "DiscountUsages" ("DiscountCodeId");

CREATE INDEX "IX_DiscountUsages_OrderId" ON "DiscountUsages" ("OrderId");

CREATE INDEX "IX_DiscountUsages_UserId" ON "DiscountUsages" ("UserId");

CREATE INDEX "IX_ElasticsearchOutboxMessages_EntityType_EntityId" ON "ElasticsearchOutboxMessages" ("EntityType", "EntityId");

CREATE INDEX "IX_ElasticsearchOutboxMessages_ProcessedAt" ON "ElasticsearchOutboxMessages" ("ProcessedAt");

CREATE INDEX "IX_FailedElasticOperations_EntityType_EntityId" ON "FailedElasticOperations" ("EntityType", "EntityId");

CREATE INDEX "IX_FailedElasticOperations_Status" ON "FailedElasticOperations" ("Status");

CREATE UNIQUE INDEX "IX_Inventories_VariantId" ON "Inventories" ("VariantId");

CREATE INDEX "IX_Medias_EntityType_EntityId" ON "Medias" ("EntityType", "EntityId");

CREATE INDEX "IX_Notifications_UserId" ON "Notifications" ("UserId");

CREATE INDEX "IX_Notifications_UserId_IsRead" ON "Notifications" ("UserId", "IsRead");

CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");

CREATE INDEX "IX_OrderItems_OrderId1" ON "OrderItems" ("OrderId1");

CREATE INDEX "IX_OrderItems_ProductId" ON "OrderItems" ("ProductId");

CREATE INDEX "IX_OrderItems_VariantId" ON "OrderItems" ("VariantId");

CREATE UNIQUE INDEX "IX_OrderProcessStates_OrderId" ON "OrderProcessStates" ("OrderId");

CREATE INDEX "IX_Orders_AppliedDiscountCodeId" ON "Orders" ("AppliedDiscountCodeId");

CREATE UNIQUE INDEX "IX_Orders_IdempotencyKey" ON "Orders" ("IdempotencyKey");

CREATE UNIQUE INDEX "IX_Orders_OrderNumber" ON "Orders" ("OrderNumber");

CREATE INDEX "IX_Orders_UserId" ON "Orders" ("UserId");

CREATE UNIQUE INDEX "IX_OrderStatuses_Name" ON "OrderStatuses" ("Name");

CREATE INDEX "IX_OutboxMessages_processed_at" ON "OutboxMessages" (processed_at);

CREATE UNIQUE INDEX "IX_PaymentTransactions_Authority" ON "PaymentTransactions" ("Authority");

CREATE UNIQUE INDEX "IX_PaymentTransactions_OrderId" ON "PaymentTransactions" ("OrderId");

CREATE INDEX "IX_PaymentTransactions_UserId" ON "PaymentTransactions" ("UserId");

CREATE INDEX "IX_ProductReviews_CreatedAt" ON "ProductReviews" ("CreatedAt");

CREATE INDEX "IX_ProductReviews_OrderId" ON "ProductReviews" ("OrderId");

CREATE INDEX "IX_ProductReviews_ProductId" ON "ProductReviews" ("ProductId");

CREATE INDEX "IX_ProductReviews_UserId" ON "ProductReviews" ("UserId");

CREATE INDEX "IX_Products_BrandId" ON "Products" ("BrandId");

CREATE INDEX "IX_Products_IsActive" ON "Products" ("IsActive");

CREATE INDEX "IX_Products_IsDeleted" ON "Products" ("IsDeleted");

CREATE UNIQUE INDEX "IX_Products_Slug" ON "Products" ("Slug");

CREATE INDEX "IX_ProductVariantAttributes_AttributeTypeId" ON "ProductVariantAttributes" ("AttributeTypeId");

CREATE INDEX "IX_ProductVariantAttributes_AttributeValueId" ON "ProductVariantAttributes" ("AttributeValueId");

CREATE INDEX "IX_ProductVariantAttributes_ValueId" ON "ProductVariantAttributes" ("ValueId");

CREATE UNIQUE INDEX "IX_ProductVariantAttributes_VariantId_ValueId" ON "ProductVariantAttributes" ("VariantId", "ValueId");

CREATE INDEX "IX_ProductVariants_IsActive" ON "ProductVariants" ("IsActive");

CREATE INDEX "IX_ProductVariants_ProductId" ON "ProductVariants" ("ProductId");

CREATE INDEX "IX_ProductVariants_ProductId1" ON "ProductVariants" ("ProductId1");

CREATE UNIQUE INDEX "IX_ProductVariants_Sku" ON "ProductVariants" ("Sku");

CREATE INDEX "IX_ProductVariantShippings_ShippingId" ON "ProductVariantShippings" ("ShippingId");

CREATE INDEX "IX_ProductVariantShippings_VariantId" ON "ProductVariantShippings" ("VariantId");

CREATE UNIQUE INDEX "IX_RateLimitEntries_Key_WindowKey" ON "RateLimitEntries" ("Key", "WindowKey");

CREATE INDEX "IX_Shippings_IsActive" ON "Shippings" ("IsActive");

CREATE INDEX "IX_Shippings_IsDefault" ON "Shippings" ("IsDefault");

CREATE UNIQUE INDEX "IX_Shippings_Name" ON "Shippings" ("Name");

CREATE UNIQUE INDEX "IX_StockLedgerEntries_IdempotencyKey" ON "StockLedgerEntries" ("IdempotencyKey");

CREATE INDEX "IX_StockLedgerEntries_InventoryId" ON "StockLedgerEntries" ("InventoryId");

CREATE INDEX "IX_StockLedgerEntries_OrderItemId" ON "StockLedgerEntries" ("OrderItemId");

CREATE INDEX "IX_StockLedgerEntries_UserId" ON "StockLedgerEntries" ("UserId");

CREATE INDEX "IX_StockLedgerEntries_VariantId" ON "StockLedgerEntries" ("VariantId");

CREATE INDEX "IX_StockLedgerEntries_WarehouseId" ON "StockLedgerEntries" ("WarehouseId");

CREATE INDEX "IX_TicketMessages_TicketId" ON "TicketMessages" ("TicketId");

CREATE INDEX "IX_Tickets_AssignedAgentId" ON "Tickets" ("AssignedAgentId");

CREATE INDEX "IX_Tickets_CustomerId" ON "Tickets" ("CustomerId");

CREATE INDEX "IX_Tickets_Status" ON "Tickets" ("Status");

CREATE INDEX "IX_Tickets_UserId" ON "Tickets" ("UserId");

CREATE INDEX "IX_UserAddresses_UserId" ON "UserAddresses" ("UserId");

CREATE INDEX "IX_UserAddresses_UserId1" ON "UserAddresses" ("UserId1");

CREATE INDEX "IX_UserOtps_CreatedAt" ON "UserOtps" ("CreatedAt");

CREATE INDEX "IX_UserOtps_UserId_Purpose_ExpiresAt" ON "UserOtps" ("UserId", "Purpose", "ExpiresAt");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE UNIQUE INDEX "IX_Users_PhoneNumber" ON "Users" ("PhoneNumber") WHERE "PhoneNumber" IS NOT NULL;

CREATE UNIQUE INDEX "IX_UserSessions_RefreshToken" ON "UserSessions" ("RefreshToken");

CREATE UNIQUE INDEX "IX_UserSessions_UserId_DeviceInfo_Active" ON "UserSessions" ("UserId", "DeviceInfo") WHERE "IsRevoked" = false;

CREATE INDEX "IX_UserSessions_UserId_IsRevoked_ExpiresAt" ON "UserSessions" ("UserId", "IsRevoked", "ExpiresAt");

CREATE UNIQUE INDEX "IX_WalletLedgerEntries_IdempotencyKey" ON "WalletLedgerEntries" ("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;

CREATE INDEX "IX_WalletLedgerEntries_UserId" ON "WalletLedgerEntries" ("UserId");

CREATE INDEX "IX_WalletLedgerEntries_WalletId" ON "WalletLedgerEntries" ("WalletId");

CREATE INDEX "IX_WalletReconciliationAudit_DetectedAt" ON "WalletReconciliationAudit" ("DetectedAt");

CREATE INDEX "IX_WalletReconciliationAudit_WalletId" ON "WalletReconciliationAudit" ("WalletId");

CREATE INDEX "IX_WalletReservations_WalletId" ON "WalletReservations" ("WalletId");

CREATE INDEX "IX_WalletReservations_WalletId_Status" ON "WalletReservations" ("WalletId", "Status");

CREATE UNIQUE INDEX "IX_Wallets_UserId" ON "Wallets" ("UserId");

CREATE UNIQUE INDEX "IX_Warehouses_Code" ON "Warehouses" ("Code");

CREATE INDEX "IX_Wishlists_ProductId" ON "Wishlists" ("ProductId");

CREATE INDEX "IX_Wishlists_UserId" ON "Wishlists" ("UserId");

CREATE UNIQUE INDEX "IX_Wishlists_UserId_ProductId" ON "Wishlists" ("UserId", "ProductId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260518005030_1', '9.0.13');

COMMIT;

