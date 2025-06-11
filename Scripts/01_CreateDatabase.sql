-- WebHS Database Creation Script
-- Tạo database và cấu trúc bảng cho hệ thống Homestay

USE master;
GO

-- Xóa database nếu đã tồn tại
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'WebHS')
BEGIN
    ALTER DATABASE WebHS SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE WebHS;
END
GO

-- Tạo database mới
CREATE DATABASE WebHS
COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE WebHS;
GO

-- ==============================================
-- BẢNG IDENTITY FRAMEWORK (ASP.NET Core Identity)
-- ==============================================

-- Bảng AspNetRoles
CREATE TABLE [dbo].[AspNetRoles] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(256) NULL,
    [NormalizedName] NVARCHAR(256) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL
);

-- Bảng AspNetUsers (Người dùng)
CREATE TABLE [dbo].[AspNetUsers] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(256) NULL,
    [NormalizedUserName] NVARCHAR(256) NULL,
    [Email] NVARCHAR(256) NULL,
    [NormalizedEmail] NVARCHAR(256) NULL,
    [EmailConfirmed] BIT NOT NULL DEFAULT 0,
    [PasswordHash] NVARCHAR(MAX) NULL,
    [SecurityStamp] NVARCHAR(MAX) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    [PhoneNumberConfirmed] BIT NOT NULL DEFAULT 0,
    [TwoFactorEnabled] BIT NOT NULL DEFAULT 0,
    [LockoutEnd] DATETIMEOFFSET(7) NULL,
    [LockoutEnabled] BIT NOT NULL DEFAULT 0,
    [AccessFailedCount] INT NOT NULL DEFAULT 0,
    
    -- Thêm các trường custom
    [FirstName] NVARCHAR(100) NOT NULL DEFAULT '',
    [LastName] NVARCHAR(100) NOT NULL DEFAULT '',
    [Bio] NVARCHAR(500) NULL,
    [ProfilePicture] NVARCHAR(MAX) NULL,
    [Address] NVARCHAR(200) NULL,
    [IsHost] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);

-- Bảng AspNetUserRoles
CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] NVARCHAR(450) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    PRIMARY KEY ([UserId], [RoleId]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);

-- Bảng AspNetUserClaims
CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

-- Bảng AspNetUserLogins
CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] NVARCHAR(450) NOT NULL,
    [ProviderKey] NVARCHAR(450) NOT NULL,
    [ProviderDisplayName] NVARCHAR(MAX) NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    PRIMARY KEY ([LoginProvider], [ProviderKey]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

-- Bảng AspNetUserTokens
CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId] NVARCHAR(450) NOT NULL,
    [LoginProvider] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(450) NOT NULL,
    [Value] NVARCHAR(MAX) NULL,
    PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

-- Bảng AspNetRoleClaims
CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [RoleId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);

-- ==============================================
-- BẢNG ĐỊA CHỈ (Location Tables)
-- ==============================================

-- Bảng Countries (Quốc gia)
CREATE TABLE [dbo].[Countries] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(10) NOT NULL
);

-- Bảng Provinces (Tỉnh/Thành phố)
CREATE TABLE [dbo].[Provinces] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(10) NOT NULL,
    [CountryId] INT NOT NULL,
    FOREIGN KEY ([CountryId]) REFERENCES [Countries]([Id])
);

-- Bảng Districts (Quận/Huyện)
CREATE TABLE [dbo].[Districts] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(10) NOT NULL,
    [ProvinceId] INT NOT NULL,
    FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces]([Id])
);

-- Bảng Wards (Phường/Xã)
CREATE TABLE [dbo].[Wards] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(10) NOT NULL,
    [DistrictId] INT NOT NULL,
    FOREIGN KEY ([DistrictId]) REFERENCES [Districts]([Id])
);

-- ==============================================
-- BẢNG TIỆN NGHI (Amenities)
-- ==============================================

-- Bảng Amenities (Tiện nghi)
CREATE TABLE [dbo].[Amenities] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    [Icon] NVARCHAR(50) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()
);

-- ==============================================
-- BẢNG HOMESTAY
-- ==============================================

-- Bảng Homestays (Nhà ở homestay)
CREATE TABLE [dbo].[Homestays] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NOT NULL,
    [Address] NVARCHAR(300) NOT NULL,
    [Ward] NVARCHAR(100) NULL,
    [District] NVARCHAR(100) NULL,
    [City] NVARCHAR(100) NOT NULL,
    [State] NVARCHAR(100) NOT NULL,
    [Country] NVARCHAR(100) NOT NULL DEFAULT 'Vietnam',
    [ZipCode] NVARCHAR(20) NOT NULL,
    [Latitude] DECIMAL(10,8) NOT NULL,
    [Longitude] DECIMAL(11,8) NOT NULL,
    [PricePerNight] DECIMAL(18,2) NOT NULL,
    [MaxGuests] INT NOT NULL,
    [Bedrooms] INT NOT NULL,
    [Bathrooms] INT NOT NULL,
    [Rules] NVARCHAR(2000) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsApproved] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NULL,
    [AverageRating] DECIMAL(3,2) NOT NULL DEFAULT 0,
    [ReviewCount] INT NOT NULL DEFAULT 0,
    [HostId] NVARCHAR(450) NOT NULL,
    FOREIGN KEY ([HostId]) REFERENCES [AspNetUsers]([Id])
);

-- Bảng HomestayImages (Hình ảnh homestay)
CREATE TABLE [dbo].[HomestayImages] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ImageUrl] NVARCHAR(500) NOT NULL,
    [Caption] NVARCHAR(200) NULL,
    [IsPrimary] BIT NOT NULL DEFAULT 0,
    [Order] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [HomestayId] INT NOT NULL,
    FOREIGN KEY ([HomestayId]) REFERENCES [Homestays]([Id]) ON DELETE CASCADE
);

-- Bảng HomestayAmenities (Liên kết homestay và tiện nghi)
CREATE TABLE [dbo].[HomestayAmenities] (
    [HomestayId] INT NOT NULL,
    [AmenityId] INT NOT NULL,
    PRIMARY KEY ([HomestayId], [AmenityId]),
    FOREIGN KEY ([HomestayId]) REFERENCES [Homestays]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([AmenityId]) REFERENCES [Amenities]([Id]) ON DELETE CASCADE
);

-- Bảng HomestayPricings (Giá theo ngày của homestay)
CREATE TABLE [dbo].[HomestayPricings] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [HomestayId] INT NOT NULL,
    [Date] DATE NOT NULL,
    [PricePerNight] DECIMAL(10,2) NOT NULL,
    [Note] NVARCHAR(200) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([HomestayId]) REFERENCES [Homestays]([Id]) ON DELETE CASCADE
);

-- Bảng BlockedDates (Ngày bị chặn không cho đặt)
CREATE TABLE [dbo].[BlockedDates] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [HomestayId] INT NOT NULL,
    [Date] DATE NOT NULL,
    [Reason] NVARCHAR(200) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([HomestayId]) REFERENCES [Homestays]([Id]) ON DELETE CASCADE
);

-- ==============================================
-- BẢNG KHUYẾN MÃI (Promotions)
-- ==============================================

-- Bảng Promotions (Khuyến mãi)
CREATE TABLE [dbo].[Promotions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Code] NVARCHAR(50) NOT NULL UNIQUE,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [Type] INT NOT NULL, -- 0: Percentage, 1: FixedAmount
    [Value] DECIMAL(18,2) NOT NULL,
    [MinOrderAmount] DECIMAL(18,2) NULL,
    [MaxDiscountAmount] DECIMAL(18,2) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [StartDate] DATETIME2(7) NOT NULL,
    [EndDate] DATETIME2(7) NOT NULL,
    [UsageLimit] INT NULL,
    [UsedCount] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedByUserId] NVARCHAR(450) NULL,
    [UserId] NVARCHAR(450) NULL
);

-- ==============================================
-- BẢNG BOOKING (Đặt phòng)
-- ==============================================

-- Bảng Bookings (Đặt phòng)
CREATE TABLE [dbo].[Bookings] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CheckInDate] DATETIME2(7) NOT NULL,
    [CheckOutDate] DATETIME2(7) NOT NULL,
    [NumberOfGuests] INT NOT NULL,
    [TotalAmount] DECIMAL(18,2) NOT NULL,
    [DiscountAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [FinalAmount] DECIMAL(18,2) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Confirmed, 2: CheckedIn, 3: Completed, 4: Cancelled, 5: Refunded
    [Notes] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [HomestayId] INT NOT NULL,
    [PromotionId] INT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]),
    FOREIGN KEY ([HomestayId]) REFERENCES [Homestays]([Id]),
    FOREIGN KEY ([PromotionId]) REFERENCES [Promotions]([Id]) ON DELETE SET NULL
);

-- ==============================================
-- BẢNG THANH TOÁN (Payments)
-- ==============================================

-- Bảng Payments (Thanh toán)
CREATE TABLE [dbo].[Payments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Amount] DECIMAL(18,2) NOT NULL,
    [PaymentMethod] INT NOT NULL, -- 0: MoMo, 1: VNPay, 2: PayPal, 3: Stripe, 4: BankTransfer, 5: Free
    [Status] INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Processing, 2: Completed, 3: Failed, 4: Cancelled, 5: Refunded
    [TransactionId] NVARCHAR(100) NULL,
    [PaymentDate] DATETIME2(7) NULL,
    [FailureReason] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UserId] NVARCHAR(450) NOT NULL,
    [BookingId] INT NOT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]),
    FOREIGN KEY ([BookingId]) REFERENCES [Bookings]([Id])
);

-- ==============================================
-- BẢNG ĐÁNH GIÁ (Reviews)
-- ==============================================

-- Bảng Reviews (Đánh giá)
CREATE TABLE [dbo].[Reviews] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Rating] INT NOT NULL CHECK ([Rating] >= 1 AND [Rating] <= 5),
    [Comment] NVARCHAR(1000) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NULL,
    [HomestayId] INT NOT NULL,
    [BookingId] INT NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    FOREIGN KEY ([HomestayId]) REFERENCES [Homestays]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([BookingId]) REFERENCES [Bookings]([Id]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id])
);

-- ==============================================
-- TẠO INDEXES
-- ==============================================

-- Indexes cho AspNetUsers
CREATE INDEX [IX_AspNetUsers_NormalizedEmail] ON [AspNetUsers] ([NormalizedEmail]);
CREATE INDEX [IX_AspNetUsers_NormalizedUserName] ON [AspNetUsers] ([NormalizedUserName]);
CREATE INDEX [IX_AspNetUsers_IsHost] ON [AspNetUsers] ([IsHost]);
CREATE INDEX [IX_AspNetUsers_CreatedAt] ON [AspNetUsers] ([CreatedAt]);

-- Indexes cho Homestays
CREATE INDEX [IX_Homestays_City] ON [Homestays] ([City]);
CREATE INDEX [IX_Homestays_IsActive] ON [Homestays] ([IsActive]);
CREATE INDEX [IX_Homestays_IsApproved] ON [Homestays] ([IsApproved]);
CREATE INDEX [IX_Homestays_HostId] ON [Homestays] ([HostId]);
CREATE INDEX [IX_Homestays_PricePerNight] ON [Homestays] ([PricePerNight]);
CREATE INDEX [IX_Homestays_AverageRating] ON [Homestays] ([AverageRating]);

-- Indexes cho Bookings
CREATE INDEX [IX_Bookings_UserId] ON [Bookings] ([UserId]);
CREATE INDEX [IX_Bookings_HomestayId] ON [Bookings] ([HomestayId]);
CREATE INDEX [IX_Bookings_Status] ON [Bookings] ([Status]);
CREATE INDEX [IX_Bookings_CheckInDate] ON [Bookings] ([CheckInDate]);
CREATE INDEX [IX_Bookings_CheckOutDate] ON [Bookings] ([CheckOutDate]);
CREATE INDEX [IX_Bookings_CreatedAt] ON [Bookings] ([CreatedAt]);

-- Indexes cho Promotions
CREATE UNIQUE INDEX [IX_Promotions_Code] ON [Promotions] ([Code]);
CREATE INDEX [IX_Promotions_IsActive] ON [Promotions] ([IsActive]);
CREATE INDEX [IX_Promotions_StartDate_EndDate] ON [Promotions] ([StartDate], [EndDate]);

-- Indexes cho HomestayPricings
CREATE UNIQUE INDEX [IX_HomestayPricings_HomestayId_Date] ON [HomestayPricings] ([HomestayId], [Date]);

-- Indexes cho BlockedDates
CREATE UNIQUE INDEX [IX_BlockedDates_HomestayId_Date] ON [BlockedDates] ([HomestayId], [Date]);

-- Indexes cho Reviews
CREATE INDEX [IX_Reviews_HomestayId] ON [Reviews] ([HomestayId]);
CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);
CREATE INDEX [IX_Reviews_BookingId] ON [Reviews] ([BookingId]);
CREATE INDEX [IX_Reviews_Rating] ON [Reviews] ([Rating]);
CREATE INDEX [IX_Reviews_CreatedAt] ON [Reviews] ([CreatedAt]);

-- ==============================================
-- THÊM FOREIGN KEY CONSTRAINTS
-- ==============================================

-- Add foreign keys for Promotions table
ALTER TABLE [Promotions] 
ADD CONSTRAINT FK_Promotions_CreatedByUser 
FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL;

ALTER TABLE [Promotions] 
ADD CONSTRAINT FK_Promotions_User 
FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL;

PRINT 'Database WebHS đã được tạo thành công!';
