-- Script để liệt kê tất cả các bảng trong database WebHS và tạo lệnh SELECT
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

-- 1. Liệt kê tất cả các bảng trong database
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;

-- 2. Tạo lệnh SELECT cho tất cả các bảng
SELECT 
    'SELECT * FROM [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '];' AS SelectCommand
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;

-- 3. Đếm số lượng bảng
SELECT COUNT(*) AS TotalTables
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';

-- ===================================================================
-- CÁC LỆNH SELECT CHO TỪNG BẢNG (Dự kiến theo DbContext):
-- ===================================================================

-- Bảng Identity của ASP.NET Core
SELECT * FROM [dbo].[AspNetUsers];
SELECT * FROM [dbo].[AspNetRoles];  
SELECT * FROM [dbo].[AspNetUserRoles];
SELECT * FROM [dbo].[AspNetUserClaims];
SELECT * FROM [dbo].[AspNetUserLogins];
SELECT * FROM [dbo].[AspNetUserTokens];
SELECT * FROM [dbo].[AspNetRoleClaims];

-- Bảng chính của ứng dụng WebHS
SELECT * FROM [dbo].[Homestays];
SELECT * FROM [dbo].[Amenities]; 
SELECT * FROM [dbo].[HomestayAmenities];
SELECT * FROM [dbo].[HomestayImages];
SELECT * FROM [dbo].[Bookings];
SELECT * FROM [dbo].[Promotions];
SELECT * FROM [dbo].[Payments];
SELECT * FROM [dbo].[BlockedDates];
SELECT * FROM [dbo].[HomestayPricings];

-- Bảng địa chỉ
SELECT * FROM [dbo].[Countries];
SELECT * FROM [dbo].[Provinces];
SELECT * FROM [dbo].[Districts]; 
SELECT * FROM [dbo].[Wards];

-- Bảng migration history
SELECT * FROM [dbo].[__EFMigrationsHistory];

-- ===================================================================
-- KIỂM TRA DỮ LIỆU CƠ BẢN:
-- ===================================================================

-- Đếm số lượng records trong từng bảng chính
SELECT 'AspNetUsers' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[AspNetUsers]
UNION ALL
SELECT 'Homestays', COUNT(*) FROM [dbo].[Homestays] 
UNION ALL
SELECT 'Bookings', COUNT(*) FROM [dbo].[Bookings]
UNION ALL
SELECT 'Amenities', COUNT(*) FROM [dbo].[Amenities]
UNION ALL
SELECT 'HomestayImages', COUNT(*) FROM [dbo].[HomestayImages]
UNION ALL
SELECT 'Payments', COUNT(*) FROM [dbo].[Payments]
UNION ALL
SELECT 'Promotions', COUNT(*) FROM [dbo].[Promotions]
UNION ALL
SELECT 'BlockedDates', COUNT(*) FROM [dbo].[BlockedDates]
ORDER BY TableName;

-- ===================================================================
-- KIỂM TRA REVIEW DATA (Đã merge vào Booking):
-- ===================================================================

-- Kiểm tra các booking có review
SELECT 
    Id,
    UserId,
    HomestayId,
    CheckInDate,
    CheckOutDate,
    ReviewRating,
    ReviewComment,
    ReviewCreatedAt,
    ReviewIsActive
FROM [dbo].[Bookings] 
WHERE ReviewRating IS NOT NULL
ORDER BY ReviewCreatedAt DESC;

-- Thống kê review theo rating
SELECT 
    ReviewRating,
    COUNT(*) AS ReviewCount
FROM [dbo].[Bookings] 
WHERE ReviewRating IS NOT NULL
GROUP BY ReviewRating
ORDER BY ReviewRating;
