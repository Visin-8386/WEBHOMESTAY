-- WebHS Database Complete Setup Script
-- Script tổng hợp để tạo toàn bộ database WebHS

USE master;
GO

PRINT '==============================================';
PRINT 'BẮT ĐẦU THIẾT LẬP DATABASE WEBHS';
PRINT '==============================================';

-- Execute the database creation script
:r "01_CreateDatabase.sql"

PRINT '';
PRINT 'Database structure created successfully!';
PRINT '';

-- Execute the sample data script  
:r "02_SampleData.sql"

PRINT '';
PRINT 'Basic sample data inserted successfully!';
PRINT '';

-- Execute the advanced sample data script
:r "03_AdvancedSampleData.sql"

PRINT '';
PRINT 'Advanced sample data inserted successfully!';
PRINT '';

PRINT '==============================================';
PRINT 'HOÀN TẤT THIẾT LẬP DATABASE WEBHS';
PRINT '==============================================';

-- Show summary statistics
USE WebHS;
GO

PRINT 'DATABASE SUMMARY:';
PRINT '- Users: ' + CAST((SELECT COUNT(*) FROM AspNetUsers) AS VARCHAR);
PRINT '- Homestays: ' + CAST((SELECT COUNT(*) FROM Homestays) AS VARCHAR);
PRINT '- Amenities: ' + CAST((SELECT COUNT(*) FROM Amenities) AS VARCHAR);
PRINT '- Bookings: ' + CAST((SELECT COUNT(*) FROM Bookings) AS VARCHAR);
PRINT '- Payments: ' + CAST((SELECT COUNT(*) FROM Payments) AS VARCHAR);
PRINT '- Reviews: ' + CAST((SELECT COUNT(*) FROM Reviews) AS VARCHAR);
PRINT '- Promotions: ' + CAST((SELECT COUNT(*) FROM Promotions) AS VARCHAR);

PRINT '';
PRINT 'Database WebHS is ready for use!';
PRINT 'Connection String: Server=localhost;Database=WebHS;Trusted_Connection=true;TrustServerCertificate=true;';
