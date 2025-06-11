-- WebHS Database Advanced Sample Data Script
-- Tạo dữ liệu booking, payment, review mẫu cho hệ thống

USE WebHS;
GO

-- ==============================================
-- THÊM DỮ LIỆU BOOKINGS MẪU
-- ==============================================

-- Past completed bookings
INSERT INTO [Bookings] (
    [CheckInDate], [CheckOutDate], [NumberOfGuests], [TotalAmount], [DiscountAmount], 
    [FinalAmount], [Status], [Notes], [CreatedAt], [UserId], [HomestayId], [PromotionId]
) VALUES
-- Booking 1: Villa Luxury Đà Lạt (Completed)
(DATEADD(DAY, -45, GETUTCDATE()), DATEADD(DAY, -42, GETUTCDATE()), 4, 7500000.00, 0, 7500000.00, 
 3, 'Kỳ nghỉ gia đình tuyệt vời', DATEADD(DAY, -50, GETUTCDATE()), 'user-001', 1, NULL),

-- Booking 2: Căn hộ Sài Gòn (Completed with promotion)
(DATEADD(DAY, -30, GETUTCDATE()), DATEADD(DAY, -28, GETUTCDATE()), 2, 3600000.00, 200000.00, 3400000.00,
 3, 'Honeymoon tại TP.HCM', DATEADD(DAY, -35, GETUTCDATE()), 'user-002', 2, 1),

-- Booking 3: Biệt thự Nha Trang (Completed)
(DATEADD(DAY, -60, GETUTCDATE()), DATEADD(DAY, -56, GETUTCDATE()), 6, 12800000.00, 0, 12800000.00,
 3, 'Du lịch nhóm bạn', DATEADD(DAY, -65, GETUTCDATE()), 'user-003', 3, NULL),

-- Booking 4: Nhà phố Hà Nội (Completed)
(DATEADD(DAY, -25, GETUTCDATE()), DATEADD(DAY, -23, GETUTCDATE()), 3, 3000000.00, 0, 3000000.00,
 3, 'Khám phá phố cổ', DATEADD(DAY, -30, GETUTCDATE()), 'user-004', 4, NULL),

-- Booking 5: Resort Phú Quốc (Completed with promotion)
(DATEADD(DAY, -40, GETUTCDATE()), DATEADD(DAY, -37, GETUTCDATE()), 8, 8400000.00, 500000.00, 7900000.00,
 3, 'Kỳ nghỉ gia đình mở rộng', DATEADD(DAY, -45, GETUTCDATE()), 'user-005', 5, 2),

-- Current active bookings (checked in)
-- Booking 6: Villa Đà Lạt (Currently checked in)
(DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, 2, GETUTCDATE()), 6, 10000000.00, 0, 10000000.00,
 2, 'Đang lưu trú', DATEADD(DAY, -10, GETUTCDATE()), 'user-001', 1, NULL),

-- Booking 7: Căn hộ Sài Gòn (Currently checked in)
(DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, 3, GETUTCDATE()), 2, 7200000.00, 0, 7200000.00,
 2, 'Business trip', DATEADD(DAY, -8, GETUTCDATE()), 'user-002', 2, NULL),

-- Future confirmed bookings
-- Booking 8: Biệt thự Nha Trang (Future)
(DATEADD(DAY, 15, GETUTCDATE()), DATEADD(DAY, 18, GETUTCDATE()), 4, 9600000.00, 0, 9600000.00,
 1, 'Kỳ nghỉ hè', DATEADD(DAY, -5, GETUTCDATE()), 'user-003', 3, NULL),

-- Booking 9: Nhà phố Hà Nội (Future with promotion)
(DATEADD(DAY, 10, GETUTCDATE()), DATEADD(DAY, 13, GETUTCDATE()), 4, 4500000.00, 50000.00, 4450000.00,
 1, 'Weekend trip', DATEADD(DAY, -3, GETUTCDATE()), 'user-004', 4, 3),

-- Booking 10: Resort Phú Quốc (Future)
(DATEADD(DAY, 25, GETUTCDATE()), DATEADD(DAY, 30, GETUTCDATE()), 10, 14000000.00, 0, 14000000.00,
 1, 'Company retreat', DATEADD(DAY, -2, GETUTCDATE()), 'user-005', 5, NULL);

-- ==============================================
-- THÊM DỮ LIỆU PAYMENTS
-- ==============================================

INSERT INTO [Payments] (
    [Amount], [PaymentMethod], [Status], [TransactionId], [PaymentDate], 
    [CreatedAt], [UserId], [BookingId]
) VALUES
-- Payments for completed bookings
(7500000.00, 1, 2, 'VNP_TX_001_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -50, GETUTCDATE()), 
 DATEADD(DAY, -50, GETUTCDATE()), 'user-001', 1),

(3400000.00, 0, 2, 'MOMO_TX_002_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -35, GETUTCDATE()),
 DATEADD(DAY, -35, GETUTCDATE()), 'user-002', 2),

(12800000.00, 3, 2, 'STRIPE_TX_003_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -65, GETUTCDATE()),
 DATEADD(DAY, -65, GETUTCDATE()), 'user-003', 3),

(3000000.00, 1, 2, 'VNP_TX_004_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -30, GETUTCDATE()),
 DATEADD(DAY, -30, GETUTCDATE()), 'user-004', 4),

(7900000.00, 0, 2, 'MOMO_TX_005_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -45, GETUTCDATE()),
 DATEADD(DAY, -45, GETUTCDATE()), 'user-005', 5),

-- Payments for current bookings
(10000000.00, 1, 2, 'VNP_TX_006_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -10, GETUTCDATE()),
 DATEADD(DAY, -10, GETUTCDATE()), 'user-001', 6),

(7200000.00, 3, 2, 'STRIPE_TX_007_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -8, GETUTCDATE()),
 DATEADD(DAY, -8, GETUTCDATE()), 'user-002', 7),

-- Payments for future bookings
(9600000.00, 0, 2, 'MOMO_TX_008_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -5, GETUTCDATE()),
 DATEADD(DAY, -5, GETUTCDATE()), 'user-003', 8),

(4450000.00, 1, 2, 'VNP_TX_009_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -3, GETUTCDATE()),
 DATEADD(DAY, -3, GETUTCDATE()), 'user-004', 9),

(14000000.00, 3, 2, 'STRIPE_TX_010_' + CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), DATEADD(DAY, -2, GETUTCDATE()),
 DATEADD(DAY, -2, GETUTCDATE()), 'user-005', 10);

-- ==============================================
-- THÊM DỮ LIỆU REVIEWS (CHỈ CHO COMPLETED BOOKINGS)
-- ==============================================

INSERT INTO [Reviews] (
    [Rating], [Comment], [IsActive], [CreatedAt], [HomestayId], [BookingId], [UserId]
) VALUES
-- Review cho Villa Luxury Đà Lạt
(5, 'Villa tuyệt vời! View núi đẹp, không gian rộng rãi và chủ nhà rất thân thiện. Gia đình tôi đã có những ngày nghỉ tuyệt vời tại đây. Sẽ quay lại lần sau!', 
 1, DATEADD(DAY, -40, GETUTCDATE()), 1, 1, 'user-001'),

-- Review cho Căn hộ Sài Gòn
(4, 'Căn hộ đẹp, vị trí thuận tiện ở trung tâm TP.HCM. View thành phố từ ban công rất đẹp. Chỉ có điều hơi ồn về đêm do gần đường lớn.',
 1, DATEADD(DAY, -25, GETUTCDATE()), 2, 2, 'user-002'),

-- Review cho Biệt thự Nha Trang
(5, 'Không có gì để chê! Biệt thự mặt biển, hồ bơi riêng, view biển 180 độ tuyệt đẹp. Phù hợp cho nhóm bạn du lịch. Host rất tận tình hỗ trợ.',
 1, DATEADD(DAY, -52, GETUTCDATE()), 3, 3, 'user-003'),

-- Review cho Nhà phố Hà Nội
(4, 'Nhà phố cổ kính, thiết kế đẹp mắt, gần các điểm tham quan. Tuy nhiên không gian hơi nhỏ so với mô tả. Nhưng nhìn chung vẫn là một trải nghiệm tốt.',
 1, DATEADD(DAY, -20, GETUTCDATE()), 4, 4, 'user-004'),

-- Review cho Resort Phú Quốc
(5, 'Resort mini tuyệt vời với khu vườn nhiệt đới và view hoàng hôn đẹp nhất tôi từng thấy. Cách biển rất gần, có nhiều hoạt động thú vị. Highly recommended!',
 1, DATEADD(DAY, -35, GETUTCDATE()), 5, 5, 'user-005');

-- ==============================================
-- THÊM DỮ LIỆU BLOCKED DATES MẪU
-- ==============================================

INSERT INTO [BlockedDates] ([HomestayId], [Date], [Reason]) VALUES
-- Block dates for maintenance
(1, DATEADD(DAY, 5, GETUTCDATE()), 'Bảo trì hồ bơi'),
(1, DATEADD(DAY, 6, GETUTCDATE()), 'Bảo trì hồ bơi'),
(2, DATEADD(DAY, 8, GETUTCDATE()), 'Sửa chữa điều hòa'),
(3, DATEADD(DAY, 12, GETUTCDATE()), 'Bảo trì khu vườn'),
(4, DATEADD(DAY, 20, GETUTCDATE()), 'Sơn lại nhà'),
(5, DATEADD(DAY, 22, GETUTCDATE()), 'Tổng vệ sinh resort');

-- ==============================================
-- THÊM DỮ LIỆU CUSTOM PRICING
-- ==============================================

INSERT INTO [HomestayPricings] ([HomestayId], [Date], [PricePerNight], [Note]) VALUES
-- Peak season pricing for Villa Đà Lạt
(1, DATEADD(DAY, 30, GETUTCDATE()), 3500000.00, 'Giá cao điểm mùa hè'),
(1, DATEADD(DAY, 31, GETUTCDATE()), 3500000.00, 'Giá cao điểm mùa hè'),
(1, DATEADD(DAY, 32, GETUTCDATE()), 3500000.00, 'Giá cao điểm mùa hè'),

-- Holiday pricing for Sài Gòn apartment
(2, DATEADD(DAY, 35, GETUTCDATE()), 2200000.00, 'Giá cuối tuần'),
(2, DATEADD(DAY, 36, GETUTCDATE()), 2200000.00, 'Giá cuối tuần'),

-- Festival pricing for Nha Trang villa
(3, DATEADD(DAY, 40, GETUTCDATE()), 4000000.00, 'Lễ hội biển Nha Trang'),
(3, DATEADD(DAY, 41, GETUTCDATE()), 4000000.00, 'Lễ hội biển Nha Trang'),
(3, DATEADD(DAY, 42, GETUTCDATE()), 4000000.00, 'Lễ hội biển Nha Trang'),

-- Low season pricing for Hà Nội house
(4, DATEADD(DAY, 50, GETUTCDATE()), 1200000.00, 'Giá thấp điểm mùa đông'),
(4, DATEADD(DAY, 51, GETUTCDATE()), 1200000.00, 'Giá thấp điểm mùa đông'),

-- Peak season for Phú Quốc resort
(5, DATEADD(DAY, 60, GETUTCDATE()), 3500000.00, 'Mùa khô - cao điểm'),
(5, DATEADD(DAY, 61, GETUTCDATE()), 3500000.00, 'Mùa khô - cao điểm'),
(5, DATEADD(DAY, 62, GETUTCDATE()), 3500000.00, 'Mùa khô - cao điểm');

-- ==============================================
-- CẬP NHẬT SỐ LƯỢNG SỬ DỤNG PROMOTION
-- ==============================================

-- Update promotion usage count
UPDATE [Promotions] SET [UsedCount] = [UsedCount] + 1 WHERE [Id] = 1; -- WELCOME10
UPDATE [Promotions] SET [UsedCount] = [UsedCount] + 1 WHERE [Id] = 2; -- SUMMER2024
UPDATE [Promotions] SET [UsedCount] = [UsedCount] + 1 WHERE [Id] = 3; -- WEEKEND50

-- ==============================================
-- CẬP NHẬT REVIEW COUNT VÀ RATING CHO HOMESTAYS
-- ==============================================

-- Recalculate actual ratings based on inserted reviews
DECLARE @homestayId INT, @avgRating DECIMAL(3,2), @reviewCount INT;

DECLARE homestay_cursor CURSOR FOR 
SELECT DISTINCT HomestayId FROM Reviews WHERE IsActive = 1;

OPEN homestay_cursor;
FETCH NEXT FROM homestay_cursor INTO @homestayId;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT 
        @avgRating = CAST(AVG(CAST(Rating AS DECIMAL(3,2))) AS DECIMAL(3,2)),
        @reviewCount = COUNT(*)
    FROM Reviews 
    WHERE HomestayId = @homestayId AND IsActive = 1;
    
    UPDATE Homestays 
    SET AverageRating = @avgRating, ReviewCount = @reviewCount
    WHERE Id = @homestayId;
    
    FETCH NEXT FROM homestay_cursor INTO @homestayId;
END

CLOSE homestay_cursor;
DEALLOCATE homestay_cursor;

PRINT 'Dữ liệu booking, payment, review đã được thêm thành công!';
PRINT 'Rating và review count đã được cập nhật!';

-- ==============================================
-- THÊM THÊM REVIEWS ĐÁNH GIÁ ĐA DẠNG
-- ==============================================

-- Thêm một số reviews ảo để có đủ dữ liệu test
DECLARE @i INT = 1;
DECLARE @maxBookings INT;

SELECT @maxBookings = COUNT(*) FROM Bookings WHERE Status = 3; -- Completed bookings

-- Add more sample reviews for variety
INSERT INTO [Reviews] (
    [Rating], [Comment], [IsActive], [CreatedAt], [HomestayId], [BookingId], [UserId]
) VALUES
-- Additional reviews for better rating distribution
(4, 'Homestay tốt, tuy nhiên có một số điểm cần cải thiện về dịch vụ. Nhìn chung vẫn đáng tiền.', 
 1, DATEADD(DAY, -15, GETUTCDATE()), 1, 1, 'user-002'),
 
(5, 'Tuyệt vời! Đây là lần thứ hai tôi ở đây và vẫn rất hài lòng. Sẽ giới thiệu cho bạn bè.', 
 1, DATEADD(DAY, -12, GETUTCDATE()), 2, 2, 'user-003'),
 
(3, 'Vị trí tốt nhưng tiện nghi hơi cũ. Giá cả hợp lý, phù hợp cho ngân sách vừa phải.',
 1, DATEADD(DAY, -10, GETUTCDATE()), 3, 3, 'user-004'),
 
(4, 'Không gian đẹp, sạch sẽ. Host nhiệt tình. Điểm trừ là wifi hơi chậm.',
 1, DATEADD(DAY, -8, GETUTCDATE()), 4, 4, 'user-005'),
 
(5, 'Perfect! Không có gì để phàn nàn. Đây là homestay tốt nhất tôi từng ở.',
 1, DATEADD(DAY, -5, GETUTCDATE()), 5, 5, 'user-001');

-- Update final ratings
UPDATE Homestays SET AverageRating = 4.7, ReviewCount = 6 WHERE Id = 1;
UPDATE Homestays SET AverageRating = 4.5, ReviewCount = 4 WHERE Id = 2;
UPDATE Homestays SET AverageRating = 4.2, ReviewCount = 8 WHERE Id = 3;
UPDATE Homestays SET AverageRating = 4.3, ReviewCount = 5 WHERE Id = 4;
UPDATE Homestays SET AverageRating = 4.8, ReviewCount = 7 WHERE Id = 5;

PRINT 'Hoàn tất tạo dữ liệu mẫu nâng cao!';
