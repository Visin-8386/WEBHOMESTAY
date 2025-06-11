using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;
using WebHSUser = WebHS.Models.User;
using WebHS.ViewModels;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSPromotionType = WebHS.Models.PromotionType;

namespace WebHS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromotionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, string status = "")
        {
            var pageSize = 20;
            var skip = (page - 1) * pageSize;

            var query = _context.Promotions.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);
                        break;
                    case "expired":
                        query = query.Where(p => p.EndDate < DateTime.UtcNow);
                        break;
                    case "upcoming":
                        query = query.Where(p => p.StartDate > DateTime.UtcNow);
                        break;
                    case "inactive":
                        query = query.Where(p => !p.IsActive);
                        break;
                }
            }

            var totalPromotions = await query.CountAsync();
            var promotions = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(p => new PromotionViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Description = p.Description ?? string.Empty,
                    Type = p.Type,
                    Value = p.Value,
                    MinOrderAmount = p.MinOrderAmount,
                    MaxDiscountAmount = p.MaxDiscountAmount,
                    UsageLimit = p.UsageLimit,
                    UsedCount = p.UsedCount,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            var model = new PromotionListViewModel
            {
                Promotions = promotions,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalPromotions / pageSize),
                TotalCount = totalPromotions
            };

            ViewBag.Status = status;
            return View(model);
        }

        public IActionResult Create()
        {
            return View(new CreatePromotionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePromotionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if code already exists
            var existingPromotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == model.Code);

            if (existingPromotion != null)
            {
                ModelState.AddModelError("Code", "Mã khuyến mãi đã tồn tại");
                return View(model);
            }

            var promotion = new WebHSPromotion
            {
                Name = model.Name,
                Code = model.Code.ToUpper(),
                Description = model.Description,
                Type = model.Type,
                Value = model.Value,
                MinOrderAmount = model.MinOrderAmount,
                MaxDiscountAmount = model.MaxDiscountAmount,
                UsageLimit = model.UsageLimit,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Khuyến mãi đã được tạo thành công";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return NotFound();

            var model = new EditPromotionViewModel
            {
                Id = promotion.Id,
                Name = promotion.Name,
                Code = promotion.Code,
                Description = promotion.Description ?? string.Empty,
                Type = promotion.Type,
                Value = promotion.Value,
                MinOrderAmount = promotion.MinOrderAmount,
                MaxDiscountAmount = promotion.MaxDiscountAmount,
                UsageLimit = promotion.UsageLimit,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPromotionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var promotion = await _context.Promotions.FindAsync(model.Id);
            if (promotion == null)
                return NotFound();

            // Check if code already exists (except current promotion)
            var existingPromotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == model.Code && p.Id != model.Id);

            if (existingPromotion != null)
            {
                ModelState.AddModelError("Code", "Mã khuyến mãi đã tồn tại");
                return View(model);
            }

            promotion.Name = model.Name;
            promotion.Code = model.Code.ToUpper();
            promotion.Description = model.Description;
            promotion.Type = model.Type;
            promotion.Value = model.Value;
            promotion.MinOrderAmount = model.MinOrderAmount;
            promotion.MaxDiscountAmount = model.MaxDiscountAmount;
            promotion.UsageLimit = model.UsageLimit;
            promotion.StartDate = model.StartDate;
            promotion.EndDate = model.EndDate;
            promotion.IsActive = model.IsActive;
            promotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Khuyến mãi đã được cập nhật thành công";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });

            // Check if promotion is being used in any bookings
            var isUsed = await _context.Bookings.AnyAsync(b => b.PromotionId == id);
            if (isUsed)
            {
                return Json(new { success = false, message = "Không thể xóa khuyến mãi đã được sử dụng" });
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Khuyến mãi đã được xóa thành công" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });

            promotion.IsActive = !promotion.IsActive;
            promotion.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var status = promotion.IsActive ? "kích hoạt" : "vô hiệu hóa";
            return Json(new { success = true, message = $"Khuyến mãi đã được {status} thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> ValidatePromotion(string code, decimal orderAmount)
        {
            if (string.IsNullOrEmpty(code))
                return Json(new { valid = false, message = "Vui lòng nhập mã khuyến mãi" });

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == code.ToUpper() && 
                                        p.IsActive &&
                                        p.StartDate <= DateTime.UtcNow &&
                                        p.EndDate >= DateTime.UtcNow &&
                                        (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value) &&
                                        (!p.MinOrderAmount.HasValue || orderAmount >= p.MinOrderAmount.Value));

            if (promotion == null)
                return Json(new { valid = false, message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn" });

            var discountAmount = promotion.Type == WebHSPromotionType.Percentage
                ? orderAmount * (promotion.Value / 100)
                : promotion.Value;

            if (promotion.MaxDiscountAmount.HasValue && discountAmount > promotion.MaxDiscountAmount.Value)
                discountAmount = promotion.MaxDiscountAmount.Value;

            return Json(new 
            { 
                valid = true, 
                message = "Mã khuyến mãi hợp lệ",
                discountAmount = discountAmount,
                promotionName = promotion.Name,
                promotionDescription = promotion.Description
            });
        }

        public async Task<IActionResult> Statistics()
        {
            var totalPromotions = await _context.Promotions.CountAsync();
            var activePromotions = await _context.Promotions
                .CountAsync(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);
            
            var totalUsage = await _context.Promotions.SumAsync(p => p.UsedCount);
            var totalDiscount = await _context.Bookings
                .Where(b => b.PromotionId.HasValue)
                .SumAsync(b => b.DiscountAmount);

            var topPromotions = await _context.Promotions
                .OrderByDescending(p => p.UsedCount)
                .Take(10)
                .Select(p => new PromotionStatViewModel
                {
                    Name = p.Name,
                    Code = p.Code,
                    UsedCount = p.UsedCount,
                    TotalDiscount = _context.Bookings
                        .Where(b => b.PromotionId == p.Id)
                        .Sum(b => b.DiscountAmount)
                })
                .ToListAsync();

            var model = new PromotionStatisticsViewModel
            {
                TotalPromotions = totalPromotions,
                ActivePromotions = activePromotions,
                TotalUsage = totalUsage,
                TotalDiscount = totalDiscount,
                TopPromotions = topPromotions
            };

            return View(model);
        }
    }
}


