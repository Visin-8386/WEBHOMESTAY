using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;
using WebHS.ViewModels;
using WebHS.Services;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSPromotionType = WebHS.Models.PromotionType;

namespace WebHS.Services
{
    public interface IBookingService
    {
        Task<BookingDetailViewModel?> CreateBookingAsync(BookingViewModel model, string userId);
        Task<BookingListViewModel> GetUserBookingsAsync(string userId, string status = "all", int page = 1);
        Task<HostBookingListViewModel> GetHostBookingsAsync(string hostId, string status = "all", int page = 1);
        Task<BookingDetailViewModel?> GetBookingDetailAsync(int id, string userId);
        Task<bool> CancelBookingAsync(int id, string userId);
        Task<bool> ConfirmBookingAsync(int id);
        Task<decimal> CalculateBookingAmount(int homestayId, DateTime checkIn, DateTime checkOut, string? promotionCode = null);
        Task<bool> IsDateAvailableAsync(int homestayId, DateTime checkIn, DateTime checkOut);
        Task<List<DateTime>> GetBookedDatesAsync(int homestayId);
    }

    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingService> _logger;
        private readonly IPricingService _pricingService;
        private const string DefaultPlaceholderImage = "/images/placeholder-homestay.svg";
        
        // Semaphore to prevent race conditions during booking creation
        private static readonly SemaphoreSlim _bookingSemaphore = new SemaphoreSlim(1, 1);

        public BookingService(ApplicationDbContext context, IEmailService emailService, ILogger<BookingService> logger, IPricingService pricingService)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _pricingService = pricingService;
        }

        public async Task<BookingDetailViewModel?> CreateBookingAsync(BookingViewModel model, string userId)
        {
            // Use semaphore to prevent race conditions
            await _bookingSemaphore.WaitAsync();
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _logger.LogInformation("Creating booking for user {UserId}, homestay {HomestayId}", userId, model.HomestayId);

                    // Validate dates
                    if (model.CheckInDate >= model.CheckOutDate || model.CheckInDate < DateTime.Today)
                    {
                        _logger.LogWarning("Invalid booking dates: CheckIn={CheckIn}, CheckOut={CheckOut}", model.CheckInDate, model.CheckOutDate);
                        return null;
                    }

                    // ADDED: Validate minimum stay requirement
                    var numberOfNights = (model.CheckOutDate - model.CheckInDate).Days;
                    if (numberOfNights < 1)
                    {
                        _logger.LogWarning("Booking does not meet minimum stay requirement: {NumberOfNights} nights", numberOfNights);
                        return null;
                    }

                    // Double-check availability within transaction to prevent race conditions
                    var isAvailable = await IsDateAvailableAsync(model.HomestayId, model.CheckInDate, model.CheckOutDate);
                    if (!isAvailable)
                    {
                        _logger.LogWarning("Homestay {HomestayId} not available for dates {CheckIn} to {CheckOut}", model.HomestayId, model.CheckInDate, model.CheckOutDate);
                        return null;
                    }

                var homestay = await _context.Homestays
                    .Include(h => h.Host)
                    .Include(h => h.Images)
                    .FirstOrDefaultAsync(h => h.Id == model.HomestayId && h.IsActive && h.IsApproved);

                if (homestay == null)
                {
                    _logger.LogWarning("Homestay {HomestayId} not found or not available", model.HomestayId);
                    return null;
                }

                // Validate guest count
                if (model.NumberOfGuests > homestay.MaxGuests)
                {
                    _logger.LogWarning("Guest count {GuestCount} exceeds max capacity {MaxGuests} for homestay {HomestayId}", 
                        model.NumberOfGuests, homestay.MaxGuests, model.HomestayId);
                    return null;
                }

                // Calculate pricing using PricingService
                var subTotal = await _pricingService.CalculateTotalPriceAsync(model.HomestayId, model.CheckInDate, model.CheckOutDate);

                WebHSPromotion? promotion = null;
                decimal discountAmount = 0;

                if (!string.IsNullOrEmpty(model.PromotionCode))
                {
                    promotion = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.Code == model.PromotionCode &&
                                                p.IsActive &&
                                                p.StartDate <= DateTime.UtcNow &&
                                                p.EndDate >= DateTime.UtcNow &&
                                                (p.UsageLimit == null || p.UsedCount < p.UsageLimit));

                    if (promotion != null)
                    {
                        discountAmount = promotion.Type == WebHSPromotionType.Percentage
                            ? subTotal * (promotion.Value / 100)
                            : promotion.Value;

                        discountAmount = Math.Min(discountAmount, subTotal);
                        _logger.LogInformation("Applied promotion {PromotionCode} with discount {DiscountAmount}", model.PromotionCode, discountAmount);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid or expired promotion code: {PromotionCode}", model.PromotionCode);
                    }
                }

                var finalAmount = subTotal - discountAmount;

                var booking = new Booking
                {
                    UserId = userId,
                    HomestayId = model.HomestayId,
                    CheckInDate = model.CheckInDate,
                    CheckOutDate = model.CheckOutDate,
                    NumberOfGuests = model.NumberOfGuests,
                    TotalAmount = subTotal,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    Status = BookingStatus.Pending,
                    Notes = model.Notes,
                    PromotionId = promotion?.Id
                };

                _context.Bookings.Add(booking);

                // Update promotion usage count
                if (promotion != null)
                {
                    promotion.UsedCount++;
                    _logger.LogInformation("Updated promotion usage count for {PromotionCode}: {UsedCount}/{UsageLimit}", 
                        promotion.Code, promotion.UsedCount, promotion.UsageLimit);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Booking {BookingId} created successfully for user {UserId}", booking.Id, userId);

                // Send confirmation email
                try
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        await _emailService.SendBookingConfirmationAsync(
                            user.Email!, 
                            $"{user.FirstName} {user.LastName}",
                            homestay.Name,
                            booking.CheckInDate,
                            booking.CheckOutDate);
                        _logger.LogInformation("Confirmation email sent for booking {BookingId}", booking.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email for booking {BookingId}", booking.Id);
                    // Don't fail the booking if email fails
                }

                return new BookingDetailViewModel
                {
                    Booking = booking,
                    HomestayName = homestay.Name,
                    PrimaryImage = GetImageUrlWithFallback(homestay.Images),
                    HostName = $"{homestay.Host.FirstName} {homestay.Host.LastName}",
                    CanReview = false,
                    CanCancel = booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed,
                    HomestayImage = GetImageUrlWithFallback(homestay.Images)
                };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating booking for user {UserId}, homestay {HomestayId}", userId, model.HomestayId);
                    throw;
                }
            }
            finally
            {
                // Always release the semaphore to prevent deadlocks
                _bookingSemaphore.Release();
            }
        }

        public async Task<BookingListViewModel> GetUserBookingsAsync(string userId, string status = "all", int page = 1)
        {
            var query = _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.Homestay.Host)
                .Where(b => b.UserId == userId);

            if (status != "all")
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    query = query.Where(b => b.Status == bookingStatus);
                }
            }

            var pageSize = 10;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingDetailViewModel
                {
                    Id = b.Id,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    NumberOfGuests = b.NumberOfGuests,
                    FinalAmount = b.FinalAmount,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    DiscountAmount = b.DiscountAmount,
                    Booking = b,
                    HomestayName = b.Homestay.Name,
                    HomestayLocation = $"{b.Homestay.City}, {b.Homestay.State}",
                    PrimaryImage = GetImageUrlWithFallback(b.Homestay.Images),
                    HostName = $"{b.Homestay.Host.FirstName} {b.Homestay.Host.LastName}",
                    CanReview = b.Status == BookingStatus.CheckedOut && !b.ReviewRating.HasValue,
                    CanCancel = b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed,
                    HomestayImage = GetImageUrlWithFallback(b.Homestay.Images)
                })
                .ToListAsync();

            return new BookingListViewModel
            {
                Bookings = bookings,
                Status = status,
                Page = page,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };
        }

        public async Task<HostBookingListViewModel> GetHostBookingsAsync(string hostId, string status = "all", int page = 1)
        {
            var query = _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.User)
                .Where(b => b.Homestay.HostId == hostId);

            if (status != "all")
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    query = query.Where(b => b.Status == bookingStatus);
                }
            }

            var pageSize = 10;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingDetailViewModel
                {
                    Id = b.Id,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    NumberOfGuests = b.NumberOfGuests,
                    FinalAmount = b.FinalAmount,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    DiscountAmount = b.DiscountAmount,
                    Booking = b,
                    HomestayName = b.Homestay.Name,
                    HomestayLocation = $"{b.Homestay.City}, {b.Homestay.State}",
                    PrimaryImage = GetImageUrlWithFallback(b.Homestay.Images),
                    UserName = $"{b.User.FirstName} {b.User.LastName}",
                    UserEmail = b.User.Email ?? "",
                    UserPhone = b.User.PhoneNumber ?? "",
                    CanReview = false, // Host doesn't review guests
                    CanCancel = b.Status == BookingStatus.Pending,
                    HomestayImage = GetImageUrlWithFallback(b.Homestay.Images)
                })
                .ToListAsync();

            return new HostBookingListViewModel
            {
                Bookings = bookings,
                Status = status,
                Page = page,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };
        }

        public async Task<BookingDetailViewModel?> GetBookingDetailAsync(int id, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.Homestay.Host)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
                return null;

            return new BookingDetailViewModel
            {
                Id = booking.Id,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                FinalAmount = booking.FinalAmount,
                Status = booking.Status,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                Booking = booking,
                HomestayName = booking.Homestay.Name,
                HomestayLocation = $"{booking.Homestay.City}, {booking.Homestay.State}",
                PrimaryImage = GetImageUrlWithFallback(booking.Homestay.Images),
                HostName = $"{booking.Homestay.Host.FirstName} {booking.Homestay.Host.LastName}",
                CanReview = booking.Status == BookingStatus.CheckedOut && !booking.ReviewRating.HasValue,
                CanCancel = booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed,
                HomestayImage = GetImageUrlWithFallback(booking.Homestay.Images)
            };
        }

        public async Task<bool> CancelBookingAsync(int id, string userId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null || (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed))
                return false;

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmBookingAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return false;

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculateBookingAmount(int homestayId, DateTime checkIn, DateTime checkOut, string? promotionCode = null)
        {
            var homestay = await _context.Homestays.FindAsync(homestayId);
            if (homestay == null)
                return 0;

            // Use PricingService to calculate total amount with dynamic pricing
            var subTotal = await _pricingService.CalculateTotalPriceAsync(homestayId, checkIn, checkOut);

            if (string.IsNullOrEmpty(promotionCode))
                return subTotal;

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == promotionCode && 
                                        p.IsActive &&
                                        p.StartDate <= DateTime.UtcNow &&
                                        p.EndDate >= DateTime.UtcNow &&
                                        (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value) &&
                                        (!p.MinOrderAmount.HasValue || subTotal >= p.MinOrderAmount.Value));

            if (promotion == null)
                return subTotal;

            var discountAmount = promotion.Type == WebHSPromotionType.Percentage
                ? subTotal * (promotion.Value / 100)
                : promotion.Value;

            if (promotion.MaxDiscountAmount.HasValue && discountAmount > promotion.MaxDiscountAmount.Value)
                discountAmount = promotion.MaxDiscountAmount.Value;

            return subTotal - discountAmount;
        }

        // Enhanced availability checking with comprehensive validation
        public async Task<bool> IsDateAvailableAsync(int homestayId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                _logger.LogDebug("Checking availability for homestay {HomestayId} from {CheckIn} to {CheckOut}", homestayId, checkIn, checkOut);

                // Basic validation - prevent same-day bookings
                if (checkIn >= checkOut)
                {
                    _logger.LogDebug("Invalid date range: CheckIn {CheckIn} must be before CheckOut {CheckOut}", checkIn, checkOut);
                    return false;
                }

                // Prevent past bookings
                if (checkIn < DateTime.Today)
                {
                    _logger.LogDebug("CheckIn date {CheckIn} is in the past", checkIn);
                    return false;
                }

        // Check for overlapping bookings - FIXED: Guests leave on checkout day, so it should be available
        // Previous logic: checkIn < b.CheckOutDate && checkOut > b.CheckInDate
        // New logic: checkIn < b.CheckOutDate && checkOut > b.CheckInDate for proper overlap detection
        // But we need to exclude checkout dates since guests leave that day
        var hasConflictingBooking = await _context.Bookings
            .AnyAsync(b => b.HomestayId == homestayId &&
                         (b.Status == BookingStatus.Confirmed || 
                          b.Status == BookingStatus.CheckedIn ||
                          b.Status == BookingStatus.CheckedOut) &&
                         checkIn < b.CheckOutDate && checkOut > b.CheckInDate);

                if (hasConflictingBooking)
                {
                    _logger.LogDebug("Homestay {HomestayId} has conflicting booking for requested dates", homestayId);
                    return false;
                }

                // Check for blocked dates
                var hasBlockedDates = await _context.BlockedDates
                    .AnyAsync(bd => bd.HomestayId == homestayId &&
                                  bd.Date >= checkIn && bd.Date < checkOut);

                if (hasBlockedDates)
                {
                    _logger.LogDebug("Homestay {HomestayId} has blocked dates in requested range", homestayId);
                    return false;
                }

                _logger.LogDebug("Homestay {HomestayId} is available for requested dates", homestayId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for homestay {HomestayId}", homestayId);
                return false;
            }
        }

        public async Task<List<DateTime>> GetBookedDatesAsync(int homestayId)
        {
            try
            {
                // FIXED: Exclude checkout dates since guests leave on that day, making it available for new bookings
                // Previous logic included all dates from CheckIn to CheckOut-1, which was correct
                // But we need to ensure consistency with the availability logic
                var bookedDates = await _context.Bookings
                    .Where(b => b.HomestayId == homestayId && 
                               (b.Status == BookingStatus.Confirmed || 
                                b.Status == BookingStatus.CheckedIn ||
                                b.Status == BookingStatus.CheckedOut))
                    .SelectMany(b => Enumerable.Range(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days)
                                            .Select(offset => b.CheckInDate.Date.AddDays(offset)))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} booked dates for homestay {HomestayId} (checkout dates excluded)", bookedDates.Count, homestayId);
                return bookedDates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booked dates for homestay {HomestayId}", homestayId);
                return new List<DateTime>();
            }
        }

        // Helper method for centralized image URL handling with fallback
        private static string GetImageUrlWithFallback(IEnumerable<HomestayImage> images)
        {
            var primaryImage = images.FirstOrDefault(i => i.IsPrimary);
            if (primaryImage != null)
                return primaryImage.ImageUrl;

            var firstImage = images.FirstOrDefault();
            if (firstImage != null)
                return firstImage.ImageUrl;

            return "/images/placeholder-homestay.svg"; // Use constant instead of instance field
        }
    }
}

