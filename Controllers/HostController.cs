using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Data;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;
using WebHSUserRoles = WebHS.Models.UserRoles;

namespace WebHS.Controllers
{
    [Authorize(Roles = WebHSUserRoles.Host)]
    public class HostController : Controller
    {
        private readonly IHomestayService _homestayService;
        private readonly IBookingService _bookingService;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HostController(
            IHomestayService homestayService,
            IBookingService bookingService,
            UserManager<WebHSUser> userManager,
            ApplicationDbContext context)
        {
            _homestayService = homestayService;
            _bookingService = bookingService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User)!;
            var homestays = await _homestayService.GetHostHomestaysAsync(userId);
            var hostBookings = await _bookingService.GetHostBookingsAsync(userId, "all", 1);

            // Generate revenue chart data for the last 6 months
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyRevenue = hostBookings.Bookings
                .Where(b => (b.Booking.Status == BookingStatus.CheckedIn || b.Booking.Status == BookingStatus.CheckedOut) && b.Booking.CreatedAt >= sixMonthsAgo)
                .GroupBy(b => new { b.Booking.CreatedAt.Year, b.Booking.CreatedAt.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(b => b.Booking.FinalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var model = new HostDashboardViewModel
            {
                TotalHomestays = homestays.Count(),
                ActiveBookings = hostBookings.Bookings.Count(b => b.Booking.Status == BookingStatus.Confirmed || b.Booking.Status == BookingStatus.CheckedIn),
                TotalBookings = hostBookings.Bookings.Count(),
                PendingBookings = hostBookings.Bookings.Count(b => b.Booking.Status == BookingStatus.Pending),
                TotalEarnings = hostBookings.Bookings.Where(b => b.Booking.Status == BookingStatus.CheckedIn || b.Booking.Status == BookingStatus.CheckedOut).Sum(b => b.Booking.FinalAmount),
                ThisMonthEarnings = hostBookings.Bookings.Where(b => (b.Booking.Status == BookingStatus.CheckedIn || b.Booking.Status == BookingStatus.CheckedOut) && b.Booking.CreatedAt.Month == DateTime.UtcNow.Month).Sum(b => b.Booking.FinalAmount),
                MyHomestays = homestays,
                RecentBookings = hostBookings.Bookings.Take(5),
                RevenueChartLabels = monthlyRevenue.Select(x => $"{x.Month:D2}/{x.Year}").ToList(),
                RevenueChartData = monthlyRevenue.Select(x => x.Revenue).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Homestays()
        {
            var userId = _userManager.GetUserId(User)!;
            var homestays = await _homestayService.GetHostHomestaysAsync(userId);
            return View(homestays);
        }

        [HttpGet]
        public IActionResult CreateHomestay()
        {
            return RedirectToAction("Create", "Homestay");
        }

        [HttpGet]
        public async Task<IActionResult> ManageBookings(int page = 1)
        {
            var userId = _userManager.GetUserId(User)!;
            var bookings = await _bookingService.GetHostBookingsAsync(userId, "all", page);
            return View("Bookings", bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var result = await _bookingService.ConfirmBookingAsync(id);
            
            if (result)
            {
                TempData["Message"] = "Đã xác nhận đặt phòng thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể xác nhận đặt phòng.";
            }

            return RedirectToAction(nameof(ManageBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var result = await _bookingService.CancelBookingAsync(id, _userManager.GetUserId(User)!);
            
            if (result)
            {
                TempData["Message"] = "Đã từ chối đặt phòng thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể từ chối đặt phòng.";
            }

            return RedirectToAction(nameof(ManageBookings));
        }

        [HttpGet]
        public async Task<IActionResult> Bookings(string status = "all", int page = 1)
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Get bookings for host's homestays using the existing service
            var bookings = await _bookingService.GetHostBookingsAsync(userId, status, page);
            
            ViewBag.Status = status;
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Revenue()
        {
            var userId = _userManager.GetUserId(User)!;
            var homestays = await _homestayService.GetHostHomestaysAsync(userId);
            var hostBookings = await _bookingService.GetHostBookingsAsync(userId, "all", 1);
            
            var revenueModel = new HostRevenueViewModel
            {
                TotalRevenue = hostBookings.Bookings.Where(b => b.Booking.Status == BookingStatus.CheckedIn || b.Booking.Status == BookingStatus.CheckedOut).Sum(b => b.Booking.FinalAmount),
                ThisMonthRevenue = hostBookings.Bookings.Where(b => (b.Booking.Status == BookingStatus.CheckedIn || b.Booking.Status == BookingStatus.CheckedOut) && 
                    b.Booking.CreatedAt.Month == DateTime.UtcNow.Month && 
                    b.Booking.CreatedAt.Year == DateTime.UtcNow.Year).Sum(b => b.Booking.FinalAmount),
                LastMonthRevenue = hostBookings.Bookings.Where(b => (b.Booking.Status == BookingStatus.CheckedIn || b.Booking.Status == BookingStatus.CheckedOut) && 
                    b.Booking.CreatedAt.Month == DateTime.UtcNow.AddMonths(-1).Month && 
                    b.Booking.CreatedAt.Year == DateTime.UtcNow.AddMonths(-1).Year).Sum(b => b.Booking.FinalAmount),
                RevenueByHomestay = homestays.ToDictionary(h => h.Name, h => 
                    hostBookings.Bookings.Where(b => b.Booking.HomestayId == h.Id && (b.Booking.Status == BookingStatus.CheckedIn || b.Booking.Status == BookingStatus.CheckedOut))
                    .Sum(b => b.Booking.FinalAmount))
            };
            
            return View(revenueModel);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return await Dashboard();
        }

        [HttpGet]
        public IActionResult Profile()
        {
            // Redirect to the main Account Profile page
            return RedirectToAction("Profile", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> GetBookingDetail(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            
            // For host controller, we need to check if the booking belongs to this host's homestay
            var booking = await _bookingService.GetHostBookingsAsync(userId, "all", 1);
            var targetBooking = booking.Bookings.FirstOrDefault(b => b.Id == id);
            
            if (targetBooking == null)
                return NotFound();

            return PartialView("_BookingDetailPartial", targetBooking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInBooking(int id)
        {
            // Since IBookingService doesn't have CheckInBookingAsync, we'll update status manually
            var result = await UpdateBookingStatusAsync(id, BookingStatus.CheckedIn);
            
            return Json(new { 
                success = result, 
                message = result ? "Đã check-in thành công!" : "Không thể check-in đặt phòng này." 
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            // Since IBookingService doesn't have CompleteBookingAsync, we'll update status manually
            var result = await UpdateBookingStatusAsync(id, BookingStatus.Completed);
            
            return Json(new { 
                success = result, 
                message = result ? "Đã hoàn thành đặt phòng!" : "Không thể hoàn thành đặt phòng này." 
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id, string reason)
        {
            var result = await _bookingService.CancelBookingAsync(id, _userManager.GetUserId(User)!);
            
            return Json(new { 
                success = result, 
                message = result ? "Đã từ chối đặt phòng thành công!" : "Không thể từ chối đặt phòng." 
            });
        }

        // Helper method to update booking status
        private async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                
                // Verify the booking belongs to this host's homestay
                var booking = await _context.Bookings
                    .Include(b => b.Homestay)
                    .FirstOrDefaultAsync(b => b.Id == bookingId && b.Homestay.HostId == userId);

                if (booking == null)
                    return false;

                // Validate status transition
                if (!IsValidStatusTransition(booking.Status, newStatus))
                    return false;

                booking.Status = newStatus;
                booking.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsValidStatusTransition(BookingStatus currentStatus, BookingStatus newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                (BookingStatus.Confirmed, BookingStatus.CheckedIn) => true,
                (BookingStatus.CheckedIn, BookingStatus.Completed) => true,
                (BookingStatus.CheckedIn, BookingStatus.CheckedOut) => true,
                (BookingStatus.CheckedOut, BookingStatus.Completed) => true,
                _ => false
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var homestay = await _context.Homestays
                    .FirstOrDefaultAsync(h => h.Id == id && h.HostId == userId);

                if (homestay == null)
                    return Json(new { success = false, message = "Không tìm thấy homestay." });

                homestay.IsActive = isActive;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = isActive ? "Đã kích hoạt homestay." : "Đã tạm dừng homestay." 
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }
    }
}

