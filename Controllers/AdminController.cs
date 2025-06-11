using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Services; // ADDED: Import services
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly IHomestayService _homestayService; // ADDED
        private readonly IExcelExportService _excelExportService;

        public AdminController(ApplicationDbContext context, UserManager<WebHSUser> userManager, IHomestayService homestayService, IExcelExportService excelExportService)
        {
            _context = context;
            _userManager = userManager;
            _homestayService = homestayService; // ADDED
            _excelExportService = excelExportService;
        }

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalHomestays = await _context.Homestays.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut)
                .SumAsync(b => b.FinalAmount);

            // Thống kê khuyến mãi
            var totalPromotions = await _context.Promotions.CountAsync();
            var activePromotions = await _context.Promotions
                .CountAsync(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);

            var pendingHomestays = await _context.Homestays
                .Where(h => !h.IsApproved)
                .CountAsync();

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Revenue chart data (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyRevenue = await _context.Bookings
                .Where(b => (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut) && b.CreatedAt >= sixMonthsAgo)
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(b => b.FinalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalHomestays = totalHomestays,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                TotalPromotions = totalPromotions,
                ActivePromotions = activePromotions,
                PendingHomestays = pendingHomestays,
                RecentUsers = recentUsers,
                RecentBookings = recentBookings,
                RevenueChartLabels = monthlyRevenue.Select(x => $"{x.Month:D2}/{x.Year}").ToList(),
                RevenueChartData = monthlyRevenue.Select(x => x.Revenue).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Users(string search = "", int page = 1, string role = "")
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => (u.Email != null && u.Email.Contains(search)) || 
                                       u.FirstName.Contains(search) || 
                                       u.LastName.Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create user view models with roles
            // Create dictionary of user roles
            var userRolesDictionary = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userRolesDictionary[user.Id] = userRoles.ToList();
            }

            var viewModel = new AdminUserListViewModel
            {
                Users = users,
                UserRoles = userRolesDictionary,
                SearchTerm = search,
                SelectedRole = role,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };
            
            // Also keep the ViewBag for backward compatibility
            ViewBag.UserRoles = viewModel.UserRoles;

            return View(viewModel);
        }

        public async Task<IActionResult> Homestays(string search = "", int page = 1, string status = "")
        {
            var query = _context.Homestays
                .Include(h => h.Host)
                .Include(h => h.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(h => h.Name.Contains(search) || 
                                       h.City.Contains(search) || 
                                       h.State.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(h => !h.IsApproved);
                        break;
                    case "approved":
                        query = query.Where(h => h.IsApproved && h.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(h => !h.IsActive);
                        break;
                }
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var homestays = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminHomestayListViewModel
            {
                Homestays = homestays,
                SearchTerm = search,
                SelectedStatus = status,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            ViewBag.Status = status; // Add ViewBag for status filter display
            return View(viewModel);
        }

        public async Task<IActionResult> HomestayDetails(int id)
        {
            var homestay = await _homestayService.GetHomestayDetailForAdminAsync(id);

            if (homestay == null)
                return NotFound();

            return View(homestay);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveHomestay(int id)
        {
            var homestay = await _context.Homestays.FindAsync(id);
            if (homestay != null)
            {
                homestay.IsApproved = true;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Homestay đã được phê duyệt thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy homestay.";
            }

            return RedirectToAction(nameof(Homestays));
        }

        [HttpPost]
        public async Task<IActionResult> RejectHomestay(int id)
        {
            var homestay = await _context.Homestays.FindAsync(id);
            if (homestay != null)
            {
                homestay.IsActive = false;
                homestay.IsApproved = false;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Homestay đã bị từ chối.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy homestay.";
            }

            return RedirectToAction(nameof(Homestays));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                
                TempData["Success"] = $"Trạng thái tài khoản đã được {(user.IsActive ? "kích hoạt" : "khóa")}.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
            }

            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Bookings(string search = "", int page = 1, string status = "")
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Host)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => (b.User.Email != null && b.User.Email.Contains(search)) || 
                                       b.Homestay.Name.Contains(search) ||
                                       b.User.FirstName.Contains(search) || 
                                       b.User.LastName.Contains(search)); 
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    query = query.Where(b => b.Status == bookingStatus);
                }
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminBookingListViewModel
            {
                Bookings = bookings,
                SearchTerm = search,
                SelectedStatus = status,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            ViewBag.Status = status; // Add ViewBag for status filter display
            return View(viewModel);
        }

        public async Task<IActionResult> Reports()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var monthlyStats = new
            {
                ThisMonthBookings = await _context.Bookings.CountAsync(b => b.CreatedAt >= thisMonth),
                LastMonthBookings = await _context.Bookings.CountAsync(b => b.CreatedAt >= lastMonth && b.CreatedAt < thisMonth),
                ThisMonthRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut) && b.CreatedAt >= thisMonth)
                    .SumAsync(b => b.FinalAmount),
                LastMonthRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut) && b.CreatedAt >= lastMonth && b.CreatedAt < thisMonth)
                    .SumAsync(b => b.FinalAmount)
            };

            // Top homestays by bookings
            var topHomestays = await _context.Homestays
                .Include(h => h.Bookings)
                .OrderByDescending(h => h.Bookings.Count)
                .Take(10)
                .Select(h => new { h.Name, BookingCount = h.Bookings.Count })
                .ToListAsync();

            // Top hosts by revenue
            var topHosts = await _context.Users
                .Include(u => u.Homestays) 
                    .ThenInclude(h => h.Bookings)
                .Where(u => u.Homestays.Any()) 
                .Select(u => new { 
                    Name = $"{u.FirstName} {u.LastName}", 
                    Revenue = u.Homestays.SelectMany(h => h.Bookings) 
                        .Where(b => b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut)
                        .Sum(b => b.FinalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            var viewModel = new AdminReportsViewModel
            {
                ThisMonthBookings = monthlyStats.ThisMonthBookings,
                LastMonthBookings = monthlyStats.LastMonthBookings,
                ThisMonthRevenue = monthlyStats.ThisMonthRevenue,
                LastMonthRevenue = monthlyStats.LastMonthRevenue,
                TopHomestays = topHomestays,
                TopHosts = topHosts
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles(string userId, List<string> roles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Get current roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                
                // Remove all current roles
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Add new roles
                if (roles != null && roles.Any())
                {
                    await _userManager.AddToRolesAsync(user, roles);
                    
                    // Update IsHost flag based on roles
                    bool isHost = roles.Contains(UserRoles.Host);
                    if (user.IsHost != isHost)
                    {
                        user.IsHost = isHost;
                        await _userManager.UpdateAsync(user);
                    }
                }
                else
                {
                    // If no roles are selected, make sure IsHost is false
                    if (user.IsHost)
                    {
                        user.IsHost = false;
                        await _userManager.UpdateAsync(user);
                    }
                }

                return Json(new { success = true, message = "Cập nhật vai trò thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                user.IsActive = false;
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, message = "Đã khóa tài khoản thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                user.IsActive = true;
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, message = "Đã mở khóa tài khoản thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActivateHomestay(int id)
        {
            try
            {
                var homestay = await _context.Homestays.FindAsync(id);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay." });
                }

                homestay.IsActive = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã kích hoạt homestay thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateHomestay(int id)
        {
            try
            {
                var homestay = await _context.Homestays.FindAsync(id);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay." });
                }

                homestay.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã vô hiệu hóa homestay thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<IActionResult> UserDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID người dùng không hợp lệ.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Users));
            }

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            
            // Get user's homestays if they are a host
            var homestays = await _context.Homestays
                .Where(h => h.HostId == id)
                .ToListAsync();
            
            // Get user's bookings
            var bookings = await _context.Bookings
                .Include(b => b.Homestay)
                .Where(b => b.UserId == id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.UserRoles = roles.ToList();
            ViewBag.Homestays = homestays;
            ViewBag.Bookings = bookings;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRevenueTestData()
        {
            try
            {
                // Lấy user và homestay đầu tiên để test
                var testUser = await _context.Users.FirstOrDefaultAsync();
                var testHomestay = await _context.Homestays.FirstOrDefaultAsync();

                if (testUser == null || testHomestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy user hoặc homestay để tạo test data" });
                }

                // Xóa dữ liệu test cũ
                var oldTestBookings = await _context.Bookings
                    .Where(b => b.Notes != null && b.Notes.Contains("REVENUE_TEST"))
                    .ToListAsync();
                
                if (oldTestBookings.Any())
                {
                    _context.Bookings.RemoveRange(oldTestBookings);
                    await _context.SaveChangesAsync();
                }

                var testBookings = new List<Booking>
                {
                    // Booking CheckedIn #1 (3 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-3),
                        CheckOutDate = DateTime.UtcNow.AddDays(2),
                        NumberOfGuests = 2,
                        TotalAmount = 3000000,
                        DiscountAmount = 0,
                        FinalAmount = 3000000,
                        Status = BookingStatus.CheckedIn,
                        Notes = "REVENUE_TEST - CheckedIn booking 1",
                        CreatedAt = DateTime.UtcNow,
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedIn #2 (5 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-5),
                        CheckOutDate = DateTime.UtcNow.AddDays(1),
                        NumberOfGuests = 4,
                        TotalAmount = 5000000,
                        DiscountAmount = 0,
                        FinalAmount = 5000000,
                        Status = BookingStatus.CheckedIn,
                        Notes = "REVENUE_TEST - CheckedIn booking 2",
                        CreatedAt = DateTime.UtcNow,
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedOut #1 (7 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-10),
                        CheckOutDate = DateTime.UtcNow.AddDays(-7),
                        NumberOfGuests = 3,
                        TotalAmount = 7000000,
                        DiscountAmount = 0,
                        FinalAmount = 7000000,
                        Status = BookingStatus.CheckedOut,
                        Notes = "REVENUE_TEST - CheckedOut booking 1",
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedOut #2 (12 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-20),
                        CheckOutDate = DateTime.UtcNow.AddDays(-15),
                        NumberOfGuests = 6,
                        TotalAmount = 12000000,
                        DiscountAmount = 500000,
                        FinalAmount = 11500000,
                        Status = BookingStatus.CheckedOut,
                        Notes = "REVENUE_TEST - CheckedOut booking 2",
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedOut #3 (4 triệu) - tháng này
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-5),
                        CheckOutDate = DateTime.UtcNow.AddDays(-2),
                        NumberOfGuests = 2,
                        TotalAmount = 4000000,
                        DiscountAmount = 0,
                        FinalAmount = 4000000,
                        Status = BookingStatus.CheckedOut,
                        Notes = "REVENUE_TEST - CheckedOut booking 3 this month",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    }
                };

                _context.Bookings.AddRange(testBookings);
                await _context.SaveChangesAsync();

                // Tính toán total revenue để verify
                var totalRevenue = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut)
                    .SumAsync(b => b.FinalAmount);

                var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var thisMonthRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut) && b.CreatedAt >= thisMonth)
                    .SumAsync(b => b.FinalAmount);

                return Json(new { 
                    success = true, 
                    message = $"Đã tạo {testBookings.Count} booking test. Total Revenue: {totalRevenue:N0} VND, This Month: {thisMonthRevenue:N0} VND",
                    totalRevenue = totalRevenue,
                    thisMonthRevenue = thisMonthRevenue
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public async Task<IActionResult> BookingDetails(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Host)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt phòng" });
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    return Json(new { success = false, message = "Đặt phòng này không thể xác nhận" });
                }

                booking.Status = BookingStatus.Confirmed;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xác nhận đặt phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt phòng" });
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    return Json(new { success = false, message = "Đặt phòng này không thể hủy" });
                }

                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hủy đặt phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // EXCEL EXPORT ACTIONS
        [HttpGet]
        public async Task<IActionResult> ExportUsersToExcel()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                var excelData = _excelExportService.ExportUsersToExcel(users);
                
                var fileName = $"QuanLyNguoiDung_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Users");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportHomestaysToExcel()
        {
            try
            {
                var homestays = await _context.Homestays
                    .Include(h => h.Host)
                    .ToListAsync();
                var excelData = _excelExportService.ExportHomestaysToExcel(homestays);
                
                var fileName = $"QuanLyHomestay_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Homestays");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportBookingsToExcel()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Homestay)
                    .ToListAsync();
                var excelData = _excelExportService.ExportBookingsToExcel(bookings);
                
                var fileName = $"QuanLyDatPhong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Bookings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportConversationsToExcel()
        {
            try
            {
                var conversations = await _context.Conversations
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Include(c => c.LastMessageSender)
                    .Include(c => c.Messages)
                    .ToListAsync();
                var excelData = _excelExportService.ExportConversationsToExcel(conversations);
                
                var fileName = $"QuanLyHoiThoai_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Index", "Messaging");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportMessagesToExcel()
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Conversation)
                    .ToListAsync();
                var excelData = _excelExportService.ExportMessagesToExcel(messages);
                
                var fileName = $"QuanLyTinNhan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Index", "Messaging");
            }
        }

    }
}


