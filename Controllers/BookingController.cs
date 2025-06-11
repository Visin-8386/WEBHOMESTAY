using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly UserManager<WebHSUser> _userManager;

        public BookingController(IBookingService bookingService, UserManager<WebHSUser> userManager)
        {
            _bookingService = bookingService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status = "all", int page = 1)
        {
            var userId = _userManager.GetUserId(User)!;
            var bookings = await _bookingService.GetUserBookingsAsync(userId, status, page);
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var booking = await _bookingService.GetBookingDetailAsync(id, userId);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin đặt phòng không hợp lệ. Vui lòng thử lại.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            // ENHANCED: More comprehensive date validation
            if (model.CheckInDate >= model.CheckOutDate)
            {
                TempData["Error"] = "Ngày trả phòng phải sau ngày nhận phòng.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            if (model.CheckInDate < DateTime.Today)
            {
                TempData["Error"] = "Ngày nhận phòng không thể là ngày trong quá khứ.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            // ADDED: Minimum stay validation
            var numberOfNights = (model.CheckOutDate - model.CheckInDate).Days;
            if (numberOfNights < 1)
            {
                TempData["Error"] = "Thời gian lưu trú tối thiểu là 1 đêm.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            // Kiểm tra tính khả dụng của phòng
            var isAvailable = await _bookingService.IsDateAvailableAsync(model.HomestayId, model.CheckInDate, model.CheckOutDate);
            if (!isAvailable)
            {
                TempData["Error"] = "Homestay không khả dụng trong thời gian bạn chọn. Vui lòng chọn ngày khác.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            var userId = _userManager.GetUserId(User)!;
            var booking = await _bookingService.CreateBookingAsync(model, userId);

            if (booking != null)
            {
                TempData["Message"] = "Đặt phòng thành công! Vui lòng thanh toán để hoàn tất đặt phòng.";
                return RedirectToAction("Checkout", "Payment", new { bookingId = booking.Booking.Id });
            }
            else
            {
                TempData["Error"] = "Không thể đặt phòng. Vui lòng thử lại.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var success = await _bookingService.CancelBookingAsync(id, userId);
                
                if (success)
                {
                    return Json(new { success = true, message = "Booking đã được hủy thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy booking này" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculateAmount(int homestayId, DateTime checkIn, DateTime checkOut, string? promotionCode = null)
        {
            var amount = await _bookingService.CalculateBookingAmount(homestayId, checkIn, checkOut, promotionCode);
            return Json(new { amount });
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int homestayId, DateTime checkIn, DateTime checkOut)
        {
            var isAvailable = await _bookingService.IsDateAvailableAsync(homestayId, checkIn, checkOut);
            return Json(new { available = isAvailable });
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings(string status = "all", int page = 1)
        {
            var userId = _userManager.GetUserId(User)!;
            var bookings = await _bookingService.GetUserBookingsAsync(userId, status, page);
            
            ViewBag.StatusFilter = status;
            ViewBag.CurrentPage = page;
            
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookedDates(int homestayId)
        {
            try
            {
                var bookedDates = await _bookingService.GetBookedDatesAsync(homestayId);
                return Json(bookedDates);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}


