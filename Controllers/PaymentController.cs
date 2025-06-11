using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using WebHS.Data;
using WebHS.ViewModels;
using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly IConfiguration _configuration;

        public PaymentController(
            ApplicationDbContext context,
            UserManager<WebHSUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Đặt phòng không hợp lệ hoặc không tồn tại";
                return RedirectToAction("Index", "Booking");
            }

            // Check if booking is still pending (not already paid)
            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "Đặt phòng này đã được thanh toán hoặc không còn hiệu lực";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
            }

            // Check if the booking is still valid (dates)
            if (booking.CheckInDate < DateTime.Today)
            {
                TempData["Error"] = "Đặt phòng này đã hết hạn do đã qua ngày nhận phòng";
                return RedirectToAction("Index", "Booking");
            }

            // Prepare payment view model
            var viewModel = new PaymentViewModel
            {
                BookingId = booking.Id,
                HomestayName = booking.Homestay.Name,
                Amount = booking.FinalAmount,
                Description = $"Thanh toán đặt phòng {booking.Homestay.Name} từ {booking.CheckInDate:dd/MM/yyyy} đến {booking.CheckOutDate:dd/MM/yyyy} cho {booking.NumberOfGuests} khách"
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Đặt phòng không hợp lệ" });
            }

            // Check booking status (it should be pending, as we're trying to pay for it now)
            if (booking.Status != BookingStatus.Pending)
            {
                return Json(new { success = false, message = "Đặt phòng này đã được thanh toán hoặc không còn hiệu lực" });
            }

            try
            {
                // Parse payment method string to enum
                if (!Enum.TryParse<PaymentMethod>(paymentMethod, true, out var paymentMethodEnum))
                {
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                }

                // Check if there's already a pending payment for this booking
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == PaymentStatus.Pending);

                Payment payment;
                if (existingPayment != null)
                {
                    // Update existing payment if payment method has changed
                    if (existingPayment.PaymentMethod != paymentMethodEnum)
                    {
                        existingPayment.PaymentMethod = paymentMethodEnum;
                        existingPayment.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    payment = existingPayment;
                }
                else
                {
                    // Create a new payment
                    payment = new Payment
                    {
                        BookingId = bookingId,
                        UserId = userId ?? string.Empty, // Ensure non-null value
                        Amount = booking.FinalAmount,
                        PaymentMethod = paymentMethodEnum,
                        Status = PaymentStatus.Pending,
                        CreatedAt = DateTime.Now
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }

                // Generate payment URL based on method
                string paymentUrl = paymentMethod.ToLower() switch
                {
                    "momo" => GenerateMoMoPaymentUrl(payment, booking),
                    "vnpay" => GenerateVNPayPaymentUrl(payment, booking),
                    "paypal" => GeneratePayPalPaymentUrl(payment, booking),
                    "free" => ProcessFreePayment(payment, booking),
                    _ => throw new ArgumentException("Phương thức thanh toán không được hỗ trợ")
                };

                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentReturn(string paymentMethod, string paymentId, string status)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Homestay)
                .Include(p => p.Booking.User)
                .FirstOrDefaultAsync(p => p.TransactionId == paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch thanh toán";
                return RedirectToAction("Index", "Booking");
            }

            var isSuccess = ValidatePaymentReturn(paymentMethod, Request.Query);
            var viewModel = new PaymentResultViewModel
            {
                TransactionId = payment.TransactionId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                BookingId = payment.BookingId,
                HomestayName = payment.Booking?.Homestay?.Name ?? "Unknown",
                PaymentDate = DateTime.Now
            };

            if (isSuccess)
            {
                // Only process if the payment is still pending
                if (payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.CompletedAt = DateTime.Now;

                    // Update booking status
                    if (payment.Booking != null)
                    {
                        payment.Booking.Status = BookingStatus.Confirmed;
                    }

                    await _context.SaveChangesAsync();
                }

                // Set success information
                viewModel.IsSuccess = true;
                viewModel.Message = "Thanh toán thành công! Đặt phòng của bạn đã được xác nhận.";
                TempData["Success"] = viewModel.Message;
            }
            else
            {
                // Only update if the payment is still pending
                if (payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Set error information
                viewModel.IsSuccess = false;
                viewModel.Message = "Thanh toán thất bại. Vui lòng thử lại.";
                viewModel.ErrorMessage = Request.Query.ContainsKey("errorMessage") ? Request.Query["errorMessage"].ToString() : "Lỗi không xác định";
                viewModel.ErrorCode = Request.Query.ContainsKey("errorCode") ? Request.Query["errorCode"].ToString() : "";
                TempData["Error"] = viewModel.Message;
            }

            return View("Result", viewModel);
        }

        [HttpPost]
        public IActionResult PaymentNotify(string paymentMethod)
        {
            try
            {
                var isValid = ValidatePaymentNotification(paymentMethod, Request);

                if (isValid)
                {
                    // Process the notification
                    // This would typically involve updating payment status
                    // and sending confirmation emails
                    
                    return Ok("00"); // Success response
                }
                else
                {
                    return BadRequest("Invalid notification");
                }
            }
            catch (Exception)
            {
                // Log error
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Result(string transactionId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Homestay)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return RedirectToAction("Index", "Booking");
            }

            var viewModel = new PaymentResultViewModel
            {
                IsSuccess = payment.Status == PaymentStatus.Completed,
                TransactionId = payment.TransactionId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                BookingId = payment.BookingId,
                HomestayName = payment.Booking?.Homestay?.Name ?? "Unknown",
                PaymentDate = payment.CompletedAt ?? payment.CreatedAt,
                Message = payment.Status == PaymentStatus.Completed 
                    ? "Thanh toán thành công!" 
                    : "Thanh toán thất bại!"
            };

            return View(viewModel);
        }

        private string GenerateMoMoPaymentUrl(Payment payment, Booking booking)
        {
            var endpoint = _configuration["MoMo:Endpoint"];
            var partnerCode = _configuration["MoMo:PartnerCode"];
            var accessKey = _configuration["MoMo:AccessKey"];
            var secretKey = _configuration["MoMo:SecretKey"];
            
            var orderId = $"ORDER_{payment.Id}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            var orderInfo = $"Thanh toán đặt phòng #{booking.Id}";
            var redirectUrl = Url.Action("PaymentReturn", "Payment", new { paymentMethod = "momo" }, Request.Scheme);
            var ipnUrl = Url.Action("PaymentNotify", "Payment", new { paymentMethod = "momo" }, Request.Scheme);
            var amount = payment.Amount.ToString("F0");
            var requestId = orderId;
            var requestType = "captureWallet";
            var extraData = "";

            // Create signature
            var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            var signature = HmacSHA256(rawSignature, secretKey ?? "");

            var requestData = new
            {
                partnerCode = partnerCode,
                partnerName = "WebHS",
                storeId = "MomoTestStore",
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };

            // In a real implementation, you would make an HTTP POST request to MoMo API
            // and return the payment URL from the response
            payment.TransactionId = orderId;
            _context.SaveChanges();

            return $"{endpoint}?orderId={orderId}&amount={amount}";
        }

        private string GenerateVNPayPaymentUrl(Payment payment, Booking booking)
        {
            var vnp_TmnCode = _configuration["VNPay:TmnCode"];
            var vnp_HashSecret = _configuration["VNPay:HashSecret"];
            var vnp_Url = _configuration["VNPay:Url"];
            var vnp_ReturnUrl = Url.Action("PaymentReturn", "Payment", new { paymentMethod = "vnpay" }, Request.Scheme);

            var vnp_TxnRef = $"{payment.Id}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            var vnp_OrderInfo = $"Thanh toan dat phong #{booking.Id}";
            var vnp_OrderType = "other";
            var vnp_Amount = (payment.Amount * 100).ToString("F0"); // VNPay requires amount in VND cents
            var vnp_Locale = "vn";
            var vnp_BankCode = "";
            var vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var vnp_CurrCode = "VND";
            var vnp_IpAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var vnp_Version = "2.1.0";
            var vnp_Command = "pay";

            var vnpData = new SortedDictionary<string, string>
            {
                {"vnp_Version", vnp_Version},
                {"vnp_Command", vnp_Command},
                {"vnp_TmnCode", vnp_TmnCode ?? ""},
                {"vnp_Amount", vnp_Amount},
                {"vnp_CreateDate", vnp_CreateDate},
                {"vnp_CurrCode", vnp_CurrCode},
                {"vnp_IpAddr", vnp_IpAddr},
                {"vnp_Locale", vnp_Locale},
                {"vnp_OrderInfo", vnp_OrderInfo},
                {"vnp_OrderType", vnp_OrderType},
                {"vnp_ReturnUrl", vnp_ReturnUrl ?? ""},
                {"vnp_TxnRef", vnp_TxnRef}
            };

            if (!string.IsNullOrEmpty(vnp_BankCode))
            {
                vnpData.Add("vnp_BankCode", vnp_BankCode);
            }

            var query = string.Join("&", vnpData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var vnp_SecureHash = HmacSHA512(query, vnp_HashSecret ?? "");
            
            payment.TransactionId = vnp_TxnRef;
            _context.SaveChanges();

            return $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";
        }

        private string GeneratePayPalPaymentUrl(Payment payment, Booking booking)
        {
            // PayPal integration would require PayPal SDK
            // This is a simplified example
            var clientId = _configuration["PayPal:ClientId"];
            var returnUrl = Url.Action("PaymentReturn", "Payment", new { paymentMethod = "paypal" }, Request.Scheme);
            var cancelUrl = Url.Action("Details", "Booking", new { id = booking.Id }, Request.Scheme);

            payment.TransactionId = $"PAYPAL_{payment.Id}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            _context.SaveChanges();

            // In real implementation, you would create PayPal payment and return the approval URL
            return $"https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token={payment.TransactionId}";
        }

        // Handle free payment method
        private string ProcessFreePayment(Payment payment, Booking booking)
        {
            // Generate a transaction ID
            var transactionId = $"FREE_{payment.Id}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            payment.TransactionId = transactionId;
            
            // Update payment status to completed immediately
            payment.Status = PaymentStatus.Completed;
            payment.CompletedAt = DateTime.Now;
            
            // Update booking status to confirmed
            if (booking != null)
            {
                booking.Status = BookingStatus.Confirmed;
            }
            
            _context.SaveChanges();
            
            // Redirect to successful payment result page
            return Url.Action("Result", "Payment", new { transactionId = transactionId }, Request.Scheme) ?? "/";
        }

        // HMAC-SHA256 Hash generator for MoMo payment
        private string HmacSHA256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private string HmacSHA512(string inputData, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private bool ValidatePaymentReturn(string paymentMethod, IQueryCollection queryParams)
        {
            try
            {
                return paymentMethod.ToLower() switch
                {
                    "momo" => ValidateMoMoReturn(queryParams),
                    "vnpay" => ValidateVNPayReturn(queryParams),
                    "paypal" => ValidatePayPalReturn(queryParams),
                    "free" => true, // Free payments are always valid
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ValidatePaymentNotification(string paymentMethod, HttpRequest request)
        {
            try
            {
                return paymentMethod.ToLower() switch
                {
                    "momo" => ValidateMoMoNotification(request),
                    "vnpay" => ValidateVNPayNotification(request),
                    "paypal" => ValidatePayPalNotification(request),
                    "free" => true, // Free payments don't need notifications
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ValidateMoMoReturn(IQueryCollection queryParams)
        {
            // MoMo validation logic
            var partnerCode = queryParams["partnerCode"].ToString();
            var orderId = queryParams["orderId"].ToString();
            var requestId = queryParams["requestId"].ToString();
            var amount = queryParams["amount"].ToString();
            var orderInfo = queryParams["orderInfo"].ToString();
            var orderType = queryParams["orderType"].ToString();
            var transId = queryParams["transId"].ToString();
            var resultCode = queryParams["resultCode"].ToString();
            var message = queryParams["message"].ToString();
            var payType = queryParams["payType"].ToString();
            var responseTime = queryParams["responseTime"].ToString();
            var extraData = queryParams["extraData"].ToString();
            var signature = queryParams["signature"].ToString();

            // Check if payment was successful
            if (resultCode != "0")
                return false;

            // Validate signature
            var secretKey = _configuration["MoMo:SecretKey"];
            var rawSignature = $"accessKey={_configuration["MoMo:AccessKey"]}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
            var expectedSignature = HmacSHA256(rawSignature, secretKey ?? "");

            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidateVNPayReturn(IQueryCollection queryParams)
        {
            // VNPay validation logic
            var vnp_ResponseCode = queryParams["vnp_ResponseCode"].ToString();
            var vnp_SecureHash = queryParams["vnp_SecureHash"].ToString();

            // Check if payment was successful
            if (vnp_ResponseCode != "00")
                return false;

            // Create sorted dictionary for signature validation
            var vnpData = new SortedDictionary<string, string>();
            foreach (var param in queryParams)
            {
                if (param.Key.StartsWith("vnp_") && param.Key != "vnp_SecureHash")
                {
                    vnpData.Add(param.Key, param.Value.ToString());
                }
            }

            // Generate signature
            var hashSecret = _configuration["VNPay:HashSecret"];
            var query = string.Join("&", vnpData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var expectedHash = HmacSHA512(query, hashSecret ?? "");

            return vnp_SecureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidatePayPalReturn(IQueryCollection queryParams)
        {
            // PayPal validation logic
            var paymentId = queryParams["paymentId"].ToString();
            var payerId = queryParams["PayerID"].ToString();
            var token = queryParams["token"].ToString();

            // In a real implementation, you would verify these with PayPal API
            // For now, just check if required parameters are present
            return !string.IsNullOrEmpty(paymentId) && !string.IsNullOrEmpty(payerId);
        }

        private bool ValidateMoMoNotification(HttpRequest request)
        {
            // MoMo IPN validation
            try
            {
                // Read request body
                using var reader = new StreamReader(request.Body);
                var body = reader.ReadToEndAsync().Result;
                
                // Parse JSON and validate signature
                // This is a simplified implementation
                return !string.IsNullOrEmpty(body);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateVNPayNotification(HttpRequest request)
        {
            // VNPay IPN validation
            try
            {
                var vnpData = new SortedDictionary<string, string>();
                foreach (var param in request.Query)
                {
                    if (param.Key.StartsWith("vnp_") && param.Key != "vnp_SecureHash")
                    {
                        vnpData.Add(param.Key, param.Value.ToString());
                    }
                }

                var vnp_SecureHash = request.Query["vnp_SecureHash"].ToString();
                var hashSecret = _configuration["VNPay:HashSecret"];
                var query = string.Join("&", vnpData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var expectedHash = HmacSHA512(query, hashSecret ?? "");

                return vnp_SecureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidatePayPalNotification(HttpRequest request)
        {
            // PayPal IPN validation
            // In a real implementation, you would verify with PayPal
            return true;
        }
    }
}

