-- WebHS Database Sample Data Script
-- Tạo dữ liệu mẫu cho hệ thống Homestay

USE WebHS;
GO

-- ==============================================
-- THÊM DỮ LIỆU ROLES
-- ==============================================

INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName]) VALUES
('1', 'Admin', 'ADMIN'),
('2', 'Host', 'HOST'),
('3', 'User', 'USER');

-- ==============================================
-- THÊM DỮ LIỆU ĐỊA CHỈ
-- ==============================================

-- Thêm Quốc gia
INSERT INTO [Countries] ([Name], [Code]) VALUES
('Vietnam', 'VN'),
('Thailand', 'TH'),
('Singapore', 'SG'),
('Malaysia', 'MY');

-- Thêm Tỉnh/Thành phố Việt Nam
INSERT INTO [Provinces] ([Name], [Code], [CountryId]) VALUES
('Hà Nội', 'HN', 1),
('Hồ Chí Minh', 'HCM', 1),
('Đà Nẵng', 'DN', 1),
('Hải Phòng', 'HP', 1),
('Cần Thơ', 'CT', 1),
('Lâm Đồng', 'LD', 1),
('Khánh Hòa', 'KH', 1),
('Quảng Nam', 'QN', 1),
('Bà Rịa - Vũng Tàu', 'VT', 1),
('Kiên Giang', 'KG', 1);

-- Thêm Quận/Huyện cho Hà Nội
INSERT INTO [Districts] ([Name], [Code], [ProvinceId]) VALUES
('Ba Đình', 'BD', 1),
('Hoàn Kiếm', 'HK', 1),
('Tây Hồ', 'TH', 1),
('Long Biên', 'LB', 1),
('Cầu Giấy', 'CG', 1),
('Đống Đa', 'DD', 1),
('Hai Bà Trưng', 'HBT', 1),
('Hoàng Mai', 'HM', 1),
('Thanh Xuân', 'TX', 1),
('Nam Từ Liêm', 'NTL', 1);

-- Thêm Quận/Huyện cho TP.HCM
INSERT INTO [Districts] ([Name], [Code], [ProvinceId]) VALUES
('Quận 1', 'Q1', 2),
('Quận 3', 'Q3', 2),
('Quận 5', 'Q5', 2),
('Quận 7', 'Q7', 2),
('Quận 10', 'Q10', 2),
('Bình Thạnh', 'BT', 2),
('Gò Vấp', 'GV', 2),
('Phú Nhuận', 'PN', 2),
('Tân Bình', 'TB', 2),
('Thủ Đức', 'TD', 2);

-- Thêm một số Phường/Xã mẫu
INSERT INTO [Wards] ([Name], [Code], [DistrictId]) VALUES
-- Hoàn Kiếm, Hà Nội
('Phường Hàng Bạc', 'HB', 2),
('Phường Hàng Bài', 'HBA', 2),
('Phường Hàng Trống', 'HT', 2),
('Phường Lý Thái Tổ', 'LTT', 2),
-- Quận 1, TP.HCM
('Phường Bến Nghé', 'BN', 11),
('Phường Bến Thành', 'BTH', 11),
('Phường Nguyễn Thái Bình', 'NTB', 11),
('Phường Phạm Ngũ Lão', 'PNL', 11);

-- ==============================================
-- THÊM DỮ LIỆU TIỆN NGHI
-- ==============================================

INSERT INTO [Amenities] ([Name], [Description], [Icon], [IsActive]) VALUES
('WiFi miễn phí', 'Internet tốc độ cao', 'fas fa-wifi', 1),
('Điều hòa', 'Hệ thống điều hòa nhiệt độ', 'fas fa-snowflake', 1),
('Tivi', 'TV màn hình phẳng', 'fas fa-tv', 1),
('Tủ lạnh', 'Tủ lạnh mini', 'fas fa-cube', 1),
('Bếp', 'Bếp nấu ăn đầy đủ', 'fas fa-utensils', 1),
('Máy giặt', 'Máy giặt tự động', 'fas fa-tshirt', 1),
('Chỗ đậu xe', 'Chỗ đậu xe miễn phí', 'fas fa-car', 1),
('Hồ bơi', 'Hồ bơi riêng hoặc chung', 'fas fa-swimmer', 1),
('Gym', 'Phòng tập thể dục', 'fas fa-dumbbell', 1),
('Ban công', 'Ban công với view đẹp', 'fas fa-building', 1),
('Vườn', 'Khu vườn riêng', 'fas fa-leaf', 1),
('BBQ', 'Khu vực nướng BBQ', 'fas fa-fire', 1),
('Thang máy', 'Thang máy trong tòa nhà', 'fas fa-elevator', 1),
('An ninh 24/7', 'Bảo vệ 24/7', 'fas fa-shield-alt', 1),
('Phòng khách riêng', 'Khu vực sinh hoạt chung', 'fas fa-couch', 1);

-- ==============================================
-- THÊM DỮ LIỆU NGƯỜI DÙNG SAMPLE
-- ==============================================

-- Admin User
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled],
    [AccessFailedCount], [FirstName], [LastName], [IsHost], [IsActive]
) VALUES (
    'admin-001', 'admin1@webhs.com', 'ADMIN1@WEBHS.COM', 'admin1@webhs.com', 'ADMIN1@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==', -- Password: Admin@123
    NEWID(), NEWID(), '+84987654321', 1, 0, 1, 0,
    'Admin', 'System', 0, 1
),
('admin-002', 'admin2@webhs.com', 'ADMIN2@WEBHS.COM', 'admin2@webhs.com', 'ADMIN2@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==', -- Password: Admin@123
    NEWID(), NEWID(), '+84987654322', 1, 0, 1, 0,
    'Admin', 'Manager', 0, 1
);

-- Host Users
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled],
    [AccessFailedCount], [FirstName], [LastName], [IsHost], [IsActive], [Bio], [Address]
) VALUES 
('host-001', 'host1@webhs.com', 'HOST1@WEBHS.COM', 'host1@webhs.com', 'HOST1@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==', -- Password: Host@123
    NEWID(), NEWID(), '+84912345001', 1, 0, 1, 0,
    'Nguyễn Văn', 'An', 1, 1, 'Chủ homestay với 5 năm kinh nghiệm', '123 Trần Phú, Đà Lạt'),
('host-002', 'host2@webhs.com', 'HOST2@WEBHS.COM', 'host2@webhs.com', 'HOST2@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84912345002', 1, 0, 1, 0,
    'Trần Thị', 'Bình', 1, 1, 'Yêu thích chia sẻ văn hóa địa phương', '456 Nguyễn Huệ, TP.HCM'),
('host-003', 'host3@webhs.com', 'HOST3@WEBHS.COM', 'host3@webhs.com', 'HOST3@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84912345003', 1, 0, 1, 0,
    'Lê Minh', 'Cường', 1, 1, 'Chuyên homestay gần biển', '789 Trần Phú, Nha Trang'),
('host-004', 'host4@webhs.com', 'HOST4@WEBHS.COM', 'host4@webhs.com', 'HOST4@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84912345004', 1, 0, 1, 0,
    'Phạm Thị', 'Dung', 1, 1, 'Homestay truyền thống phố cổ', '101 Hàng Bạc, Hà Nội'),
('host-005', 'host5@webhs.com', 'HOST5@WEBHS.COM', 'host5@webhs.com', 'HOST5@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84912345005', 1, 0, 1, 0,
    'Võ Văn', 'Em', 1, 1, 'Resort mini đảo ngọc', '234 Bãi Trường, Phú Quốc');

-- Regular Users
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], 
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled],
    [AccessFailedCount], [FirstName], [LastName], [IsHost], [IsActive]
) VALUES 
('user-001', 'user1@webhs.com', 'USER1@WEBHS.COM', 'user1@webhs.com', 'USER1@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==', -- Password: User@123
    NEWID(), NEWID(), '+84987000001', 1, 0, 1, 0,
    'Hoàng Thị', 'Lan', 0, 1),
('user-002', 'user2@webhs.com', 'USER2@WEBHS.COM', 'user2@webhs.com', 'USER2@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84987000002', 1, 0, 1, 0,
    'Đinh Văn', 'Hùng', 0, 1),
('user-003', 'user3@webhs.com', 'USER3@WEBHS.COM', 'user3@webhs.com', 'USER3@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84987000003', 1, 0, 1, 0,
    'Bùi Thị', 'Mai', 0, 1),
('user-004', 'user4@webhs.com', 'USER4@WEBHS.COM', 'user4@webhs.com', 'USER4@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84987000004', 1, 0, 1, 0,
    'Ngô Văn', 'Tú', 0, 1),
('user-005', 'user5@webhs.com', 'USER5@WEBHS.COM', 'user5@webhs.com', 'USER5@WEBHS.COM',
    1, 'AQAAAAEAACcQAAAAEC8nMiMHViaYJNNTY8pVP5MbOYnJ2QX2lKn1H9JKQ==',
    NEWID(), NEWID(), '+84987000005', 1, 0, 1, 0,
    'Lý Thị', 'Thảo', 0, 1);

-- Gán Roles cho Users
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES
-- Admin roles
('admin-001', '1'), -- Admin
('admin-002', '1'), -- Admin
-- Host roles
('host-001', '2'), -- Host
('host-002', '2'), -- Host
('host-003', '2'), -- Host
('host-004', '2'), -- Host
('host-005', '2'), -- Host
-- User roles
('user-001', '3'), -- User
('user-002', '3'), -- User
('user-003', '3'), -- User
('user-004', '3'), -- User
('user-005', '3'); -- User

-- ==============================================
-- THÊM DỮ LIỆU HOMESTAYS
-- ==============================================

INSERT INTO [Homestays] (
    [Name], [Description], [Address], [Ward], [District], [City], [State], [Country],
    [ZipCode], [Latitude], [Longitude], [PricePerNight], [MaxGuests], [Bedrooms], [Bathrooms],
    [Rules], [IsActive], [IsApproved], [HostId], [CreatedAt]
) VALUES
('Villa Luxury Đà Lạt', 
 'Villa cao cấp với view núi tuyệt đẹp, không gian rộng rãi, thoáng mát. Phù hợp cho gia đình hoặc nhóm bạn nghỉ dưỡng. Có vườn hoa, hồ bơi riêng và khu BBQ ngoài trời.',
 '123 Đường Trần Phú', 'Phường 1', 'Thành phố Đà Lạt', 'Đà Lạt', 'Lâm Đồng', 'Vietnam',
 '670000', 11.9404, 108.4583, 2500000.00, 8, 4, 3,
 'Không hút thuốc trong nhà. Không tổ chức tiệc ồn ào sau 22h. Giữ gìn vệ sinh chung.',
 1, 1, 'host-001', DATEADD(DAY, -90, GETUTCDATE())),

('Căn hộ hiện đại Sài Gòn',
 'Căn hộ 2 phòng ngủ hiện đại, view thành phố tuyệt đẹp, đầy đủ tiện nghi. Gần trung tâm mua sắm Vincom, bến Nhà Rồng và các điểm tham quan nổi tiếng.',
 '456 Nguyễn Huệ', 'Phường Bến Nghé', 'Quận 1', 'Thành phố Hồ Chí Minh', 'Hồ Chí Minh', 'Vietnam',
 '700000', 10.7769, 106.7009, 1800000.00, 4, 2, 2,
 'Check-in từ 14h, check-out trước 12h. Không mang thú cưng. Báo trước khi có khách đến thăm.',
 1, 1, 'host-002', DATEADD(DAY, -75, GETUTCDATE())),

('Biệt thự biển Nha Trang',
 'Biệt thự mặt biển với hồ bơi riêng, view biển 180 độ tuyệt đẹp. Phù hợp cho kỳ nghỉ lãng mạn hoặc gia đình. Cách bãi biển chỉ 50m, có khu vực BBQ và sân vườn.',
 '789 Trần Phú', 'Phường Lộc Thọ', 'Thành phố Nha Trang', 'Nha Trang', 'Khánh Hòa', 'Vietnam',
 '650000', 12.2388, 109.1967, 3200000.00, 6, 3, 2,
 'Bảo vệ môi trường biển. Không vứt rác xuống biển. Trẻ em phải có người lớn giám sát khi ra biển.',
 1, 1, 'host-003', DATEADD(DAY, -65, GETUTCDATE())),

('Nhà phố cổ Hà Nội',
 'Nhà phố truyền thống trong khu phố cổ, gần Hồ Hoàn Kiếm, chợ Đồng Xuân và các điểm du lịch nổi tiếng. Thiết kế kết hợp hiện đại và cổ điển, không gian ấm cúng.',
 '101 Hàng Bạc', 'Phường Hàng Bạc', 'Hoàn Kiếm', 'Hà Nội', 'Hà Nội', 'Vietnam',
 '100000', 21.0285, 105.8542, 1500000.00, 6, 3, 2,
 'Giữ yên lặng sau 23h do khu vực dân cư đông đúc. Cất giữ tài sản cẩn thận.',
 1, 1, 'host-004', DATEADD(DAY, -55, GETUTCDATE())),

('Resort mini Phú Quốc',
 'Khu nghỉ dưỡng nhỏ trên đảo Phú Quốc, cách bãi biển Bãi Trường 100m, có khu vườn tropical và hồ bơi. View hoàng hôn tuyệt đẹp, không gian riêng tư và yên tĩnh.',
 '234 Bãi Trường', 'Dương Tơ', 'Phú Quốc', 'Phú Quốc', 'Kiên Giang', 'Vietnam',
 '920000', 10.2899, 103.9840, 2800000.00, 10, 5, 4,
 'Bảo vệ sinh thái biển. Không câu cá trong khu vực bảo tồn. Check-out muộn nhất 11h.',
 1, 1, 'host-005', DATEADD(DAY, -45, GETUTCDATE()));

-- ==============================================
-- THÊM HÌNH ẢNH HOMESTAYS
-- ==============================================

INSERT INTO [HomestayImages] ([ImageUrl], [Caption], [IsPrimary], [Order], [HomestayId]) VALUES
-- Villa Luxury Đà Lạt
('/images/placeholder-homestay.svg', 'Mặt tiền villa', 1, 1, 1),
('/images/placeholder-homestay.svg', 'Phòng khách rộng rãi', 0, 2, 1),
('/images/placeholder-homestay.svg', 'Hồ bơi riêng', 0, 3, 1),
('/images/placeholder-homestay.svg', 'Khu vườn BBQ', 0, 4, 1),

-- Căn hộ hiện đại Sài Gòn
('/images/placeholder-homestay.svg', 'View thành phố từ ban công', 1, 1, 2),
('/images/placeholder-homestay.svg', 'Phòng ngủ chính', 0, 2, 2),
('/images/placeholder-homestay.svg', 'Bếp hiện đại', 0, 3, 2),

-- Biệt thự biển Nha Trang
('/images/placeholder-homestay.svg', 'View biển từ villa', 1, 1, 3),
('/images/placeholder-homestay.svg', 'Hồ bơi infinity', 0, 2, 3),
('/images/placeholder-homestay.svg', 'Bãi biển riêng', 0, 3, 3),

-- Nhà phố cổ Hà Nội
('/images/placeholder-homestay.svg', 'Mặt tiền phố cổ', 1, 1, 4),
('/images/placeholder-homestay.svg', 'Sân trong truyền thống', 0, 2, 4),
('/images/placeholder-homestay.svg', 'Phòng ngủ vintage', 0, 3, 4),

-- Resort mini Phú Quốc
('/images/placeholder-homestay.svg', 'Khu resort từ trên cao', 1, 1, 5),
('/images/placeholder-homestay.svg', 'Sunset view', 0, 2, 5),
('/images/placeholder-homestay.svg', 'Khu vườn nhiệt đới', 0, 3, 5);

-- ==============================================
-- LIÊN KẾT HOMESTAY VÀ AMENITIES
-- ==============================================

-- Villa Luxury Đà Lạt - Full amenities
INSERT INTO [HomestayAmenities] ([HomestayId], [AmenityId]) VALUES
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (1, 8), (1, 10), (1, 11), (1, 12), (1, 14);

-- Căn hộ hiện đại Sài Gòn - Urban amenities
INSERT INTO [HomestayAmenities] ([HomestayId], [AmenityId]) VALUES
(2, 1), (2, 2), (2, 3), (2, 4), (2, 5), (2, 6), (2, 7), (2, 10), (2, 13), (2, 14), (2, 15);

-- Biệt thự biển Nha Trang - Beach amenities
INSERT INTO [HomestayAmenities] ([HomestayId], [AmenityId]) VALUES
(3, 1), (3, 2), (3, 3), (3, 4), (3, 5), (3, 7), (3, 8), (3, 10), (3, 11), (3, 12), (3, 14);

-- Nhà phố cổ Hà Nội - Traditional amenities
INSERT INTO [HomestayAmenities] ([HomestayId], [AmenityId]) VALUES
(4, 1), (4, 2), (4, 3), (4, 4), (4, 5), (4, 6), (4, 15);

-- Resort mini Phú Quốc - Resort amenities
INSERT INTO [HomestayAmenities] ([HomestayId], [AmenityId]) VALUES
(5, 1), (5, 2), (5, 3), (5, 4), (5, 5), (5, 6), (5, 7), (5, 8), (5, 9), (5, 10), (5, 11), (5, 12), (5, 14);

-- ==============================================
-- THÊM DỮ LIỆU PROMOTIONS
-- ==============================================

INSERT INTO [Promotions] (
    [Code], [Name], [Description], [Type], [Value], [MinOrderAmount], [MaxDiscountAmount],
    [IsActive], [StartDate], [EndDate], [UsageLimit], [UsedCount], [CreatedByUserId]
) VALUES
('WELCOME10', 'Chào mừng khách hàng mới', 'Giảm 10% cho khách hàng đặt phòng lần đầu', 
 0, 10.00, 500000.00, 200000.00, 1, 
 DATEADD(DAY, -30, GETUTCDATE()), DATEADD(DAY, 60, GETUTCDATE()), 100, 15, 'admin-001'),

('SUMMER2024', 'Khuyến mãi mùa hè', 'Giảm 15% cho các booking trong mùa hè',
 0, 15.00, 1000000.00, 500000.00, 1,
 DATEADD(DAY, -15, GETUTCDATE()), DATEADD(DAY, 45, GETUTCDATE()), 200, 45, 'admin-001'),

('WEEKEND50', 'Giảm giá cuối tuần', 'Giảm 50,000 VNĐ cho booking cuối tuần',
 1, 50000.00, 800000.00, NULL, 1,
 DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, 30, GETUTCDATE()), 50, 12, 'admin-002');

PRINT 'Dữ liệu mẫu đã được thêm thành công!';

-- ==============================================
-- CẬP NHẬT RATING CHO HOMESTAYS (Simulation)
-- ==============================================

UPDATE [Homestays] SET 
    [AverageRating] = 4.8, 
    [ReviewCount] = 25 
WHERE [Id] = 1;

UPDATE [Homestays] SET 
    [AverageRating] = 4.6, 
    [ReviewCount] = 18 
WHERE [Id] = 2;

UPDATE [Homestays] SET 
    [AverageRating] = 4.9, 
    [ReviewCount] = 32 
WHERE [Id] = 3;

UPDATE [Homestays] SET 
    [AverageRating] = 4.4, 
    [ReviewCount] = 12 
WHERE [Id] = 4;

UPDATE [Homestays] SET 
    [AverageRating] = 4.7, 
    [ReviewCount] = 28 
WHERE [Id] = 5;

PRINT 'Cập nhật rating homestays hoàn tất!';
