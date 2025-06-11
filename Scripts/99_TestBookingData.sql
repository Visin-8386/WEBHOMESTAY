-- Test Data for Calendar Booking Issue
-- This adds a few test bookings to verify the calendar highlighting functionality

USE WebHS;
GO

-- Insert test bookings for Homestay ID 1 if they don't exist
-- These bookings will block specific dates to test calendar highlighting

DECLARE @TestUserId NVARCHAR(450);
DECLARE @HomestayId INT = 1;

-- Get a test user (or create if needed)
SELECT TOP 1 @TestUserId = Id FROM AspNetUsers WHERE Email LIKE '%user%' OR Email LIKE '%test%';

IF @TestUserId IS NULL
BEGIN
    PRINT 'No test user found. Please ensure users exist in database.';
END
ELSE
BEGIN
    PRINT 'Using test user: ' + @TestUserId;
    
    -- Clear any existing test bookings for this homestay (optional)
    -- DELETE FROM Bookings WHERE HomestayId = @HomestayId AND Notes LIKE '%TEST_BOOKING%';
    
    -- Add test bookings for the current month and next month
    DECLARE @Today DATE = GETDATE();
    DECLARE @CurrentMonth INT = MONTH(@Today);
    DECLARE @CurrentYear INT = YEAR(@Today);
    
    -- Test Booking 1: June 15-17, 2025 (Confirmed)
    IF NOT EXISTS (SELECT 1 FROM Bookings WHERE HomestayId = @HomestayId AND CheckInDate = '2025-06-15')
    BEGIN
        INSERT INTO Bookings (
            CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, DiscountAmount, 
            FinalAmount, Status, Notes, CreatedAt, UserId, HomestayId
        ) VALUES (
            '2025-06-15', '2025-06-17', 2, 500000.00, 0, 500000.00,
            1, 'TEST_BOOKING - Calendar test booking', GETUTCDATE(), @TestUserId, @HomestayId
        );
        PRINT 'Added test booking: June 15-17, 2025';
    END
    
    -- Test Booking 2: June 20-22, 2025 (Confirmed)
    IF NOT EXISTS (SELECT 1 FROM Bookings WHERE HomestayId = @HomestayId AND CheckInDate = '2025-06-20')
    BEGIN
        INSERT INTO Bookings (
            CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, DiscountAmount, 
            FinalAmount, Status, Notes, CreatedAt, UserId, HomestayId
        ) VALUES (
            '2025-06-20', '2025-06-22', 3, 750000.00, 0, 750000.00,
            1, 'TEST_BOOKING - Calendar test booking', GETUTCDATE(), @TestUserId, @HomestayId
        );
        PRINT 'Added test booking: June 20-22, 2025';
    END
    
    -- Test Booking 3: June 25-27, 2025 (CheckedIn)
    IF NOT EXISTS (SELECT 1 FROM Bookings WHERE HomestayId = @HomestayId AND CheckInDate = '2025-06-25')
    BEGIN
        INSERT INTO Bookings (
            CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, DiscountAmount, 
            FinalAmount, Status, Notes, CreatedAt, UserId, HomestayId
        ) VALUES (
            '2025-06-25', '2025-06-27', 4, 1000000.00, 0, 1000000.00,
            2, 'TEST_BOOKING - Calendar test booking', GETUTCDATE(), @TestUserId, @HomestayId
        );
        PRINT 'Added test booking: June 25-27, 2025';
    END
    
    -- Test Booking 4: July 5-8, 2025 (Confirmed)
    IF NOT EXISTS (SELECT 1 FROM Bookings WHERE HomestayId = @HomestayId AND CheckInDate = '2025-07-05')
    BEGIN
        INSERT INTO Bookings (
            CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, DiscountAmount, 
            FinalAmount, Status, Notes, CreatedAt, UserId, HomestayId
        ) VALUES (
            '2025-07-05', '2025-07-08', 2, 1125000.00, 0, 1125000.00,
            1, 'TEST_BOOKING - Calendar test booking', GETUTCDATE(), @TestUserId, @HomestayId
        );
        PRINT 'Added test booking: July 5-8, 2025';
    END
    
    PRINT 'Test bookings added successfully!';
    PRINT 'These bookings should now appear as RED/blocked dates in the calendar.';
    PRINT '';
    PRINT 'Expected blocked dates:';
    PRINT '- June 15, 16 (checkout June 17 should be available)';
    PRINT '- June 20, 21 (checkout June 22 should be available)';
    PRINT '- June 25, 26 (checkout June 27 should be available)';
    PRINT '- July 5, 6, 7 (checkout July 8 should be available)';
    PRINT '';
    PRINT 'To test: Visit the homestay details page and check if these dates show as red/booked.';
END

-- Verify the test data was added
SELECT 
    Id,
    CheckInDate,
    CheckOutDate,
    Status,
    Notes,
    CreatedAt
FROM Bookings 
WHERE HomestayId = 1 
  AND Notes LIKE '%TEST_BOOKING%'
ORDER BY CheckInDate;

PRINT '';
PRINT 'Current test bookings for Homestay 1:';
