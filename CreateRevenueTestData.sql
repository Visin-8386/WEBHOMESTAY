-- Script để tạo dữ liệu test cho doanh thu
-- Chạy script này để tạo booking data với status CheckedIn và CheckedOut

USE WebHS;
GO

-- Kiểm tra dữ liệu hiện tại
PRINT 'Current booking status distribution:';
SELECT 
    Status,
    COUNT(*) as Count,
    SUM(FinalAmount) as TotalRevenue
FROM Bookings 
GROUP BY Status
ORDER BY Status;

PRINT '';
PRINT 'Creating test bookings with revenue-generating statuses...';

-- Lấy user và homestay đầu tiên
DECLARE @TestUserId NVARCHAR(450);
DECLARE @TestHomestayId INT;

SELECT TOP 1 @TestUserId = Id FROM AspNetUsers;
SELECT TOP 1 @TestHomestayId = Id FROM Homestays;

IF @TestUserId IS NULL OR @TestHomestayId IS NULL
BEGIN
    PRINT 'ERROR: No users or homestays found!';
    RETURN;
END

-- Xóa dữ liệu test cũ
DELETE FROM Bookings WHERE Notes LIKE '%REVENUE_TEST%';

-- Tạo bookings với status CheckedIn (đang lưu trú)
INSERT INTO Bookings (
    CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, DiscountAmount, 
    FinalAmount, Status, Notes, CreatedAt, UserId, HomestayId
) VALUES
-- Booking CheckedIn #1 (3 triệu)
(DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, 2, GETUTCDATE()), 2, 3000000, 0, 3000000,
 2, 'REVENUE_TEST - CheckedIn booking 1', GETUTCDATE(), @TestUserId, @TestHomestayId),

-- Booking CheckedIn #2 (5 triệu)  
(DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, 1, GETUTCDATE()), 4, 5000000, 0, 5000000,
 2, 'REVENUE_TEST - CheckedIn booking 2', GETUTCDATE(), @TestUserId, @TestHomestayId);

-- Tạo bookings với status CheckedOut (đã trả phòng)
INSERT INTO Bookings (
    CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, DiscountAmount, 
    FinalAmount, Status, Notes, CreatedAt, UserId, HomestayId
) VALUES
-- Booking CheckedOut #1 (7 triệu)
(DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), 3, 7000000, 0, 7000000,
 3, 'REVENUE_TEST - CheckedOut booking 1', DATEADD(DAY, -10, GETUTCDATE()), @TestUserId, @TestHomestayId),

-- Booking CheckedOut #2 (12 triệu)
(DATEADD(DAY, -20, GETUTCDATE()), DATEADD(DAY, -15, GETUTCDATE()), 6, 12000000, 500000, 11500000,
 3, 'REVENUE_TEST - CheckedOut booking 2', DATEADD(DAY, -20, GETUTCDATE()), @TestUserId, @TestHomestayId),

-- Booking CheckedOut #3 (4 triệu) - tháng này
(DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 2, 4000000, 0, 4000000,
 3, 'REVENUE_TEST - CheckedOut booking 3 this month', DATEADD(DAY, -5, GETUTCDATE()), @TestUserId, @TestHomestayId);

PRINT 'Test bookings created successfully!';

-- Kiểm tra dữ liệu sau khi tạo
PRINT '';
PRINT 'Updated booking status distribution:';
SELECT 
    Status,
    CASE 
        WHEN Status = 0 THEN 'Pending'
        WHEN Status = 1 THEN 'Confirmed' 
        WHEN Status = 2 THEN 'CheckedIn'
        WHEN Status = 3 THEN 'CheckedOut'
        WHEN Status = 4 THEN 'Completed'
        WHEN Status = 5 THEN 'Cancelled'
        ELSE 'Unknown'
    END as StatusName,
    COUNT(*) as Count,
    SUM(FinalAmount) as TotalRevenue
FROM Bookings 
GROUP BY Status
ORDER BY Status;

-- Kiểm tra total revenue từ CheckedIn + CheckedOut
PRINT '';
PRINT 'Revenue calculation verification:';
SELECT 
    'CheckedIn + CheckedOut Revenue' as Description,
    SUM(FinalAmount) as TotalRevenue
FROM Bookings 
WHERE Status IN (2, 3); -- CheckedIn = 2, CheckedOut = 3

-- Kiểm tra revenue tháng này
PRINT '';
PRINT 'This month revenue (CheckedOut only):';
SELECT 
    'This Month CheckedOut Revenue' as Description,
    SUM(FinalAmount) as ThisMonthRevenue
FROM Bookings 
WHERE Status = 3 
  AND YEAR(CreatedAt) = YEAR(GETUTCDATE())
  AND MONTH(CreatedAt) = MONTH(GETUTCDATE());

PRINT '';
PRINT 'Script completed! Please refresh your admin dashboard to see updated revenue.';
PRINT 'Expected Total Revenue: ~26.5 million VND (3M + 5M + 7M + 11.5M + 4M)';
PRINT 'Expected This Month Revenue: ~4 million VND';
