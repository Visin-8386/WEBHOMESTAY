using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;
using System.Security.Claims;

namespace WebHS.Controllers
{    public class AccountController : Controller
    {
        private readonly UserManager<WebHSUser> _userManager;
        private readonly SignInManager<WebHSUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IFileUploadService _fileUploadService;

        public AccountController(
            UserManager<WebHSUser> userManager,
            SignInManager<WebHSUser> signInManager,
            IEmailService emailService,
            IFileUploadService fileUploadService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && !user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                
                if (result.RequiresTwoFactor)
                {
                    // Handle two-factor authentication if needed
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần.");
                    return View(model);
                }
                
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng xác nhận email trước khi đăng nhập.");
                    return View(model);
                }
                
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new WebHS.Models.User // Ensure this is WebHS.Models.User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address
                    // IsActive = true // Removed as it's defaulted in User model and was causing issues before
                };

                var result = await _userManager.CreateAsync(user, model.Password);                if (result.Succeeded)
                {
                    // Add user to appropriate role
                    var role = model.Role == "Host" ? WebHS.Models.UserRoles.Host : WebHS.Models.UserRoles.User;
                    await _userManager.AddToRoleAsync(user, role);

                    // Send confirmation email only in production
                    var hostEnvironment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    if (!hostEnvironment.IsDevelopment())
                    {
                        try
                        {
                            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account",
                                new { userId = user.Id, token = token }, Request.Scheme);

                            await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink!);
                            TempData["Message"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
                        }
                        catch (Exception)
                        {
                            // If email fails, still allow registration
                            TempData["Message"] = "Đăng ký thành công! Không thể gửi email xác nhận, nhưng bạn có thể đăng nhập ngay.";
                        }
                    }
                    else
                    {
                        // In development, skip email confirmation
                        TempData["Message"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
                    }

                    return RedirectToAction(nameof(Login));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["Message"] = "Email đã được xác nhận thành công! Bạn có thể đăng nhập ngay bây giờ.";
            }
            else
            {
                TempData["Error"] = "Không thể xác nhận email. Link có thể đã hết hạn.";
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action(nameof(ResetPassword), "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(resetLink))
                {
                    await _emailService.SendResetPasswordEmailAsync(user.Email, resetLink);
                }

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (userId == null || token == null)
                return BadRequest();

            var model = new ResetPasswordViewModel { UserId = userId, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Message"] = "Mật khẩu đã được đặt lại thành công!";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Bio = user.Bio,
                CurrentProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            // Check if email is already taken by another user
            if (model.Email != user.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }
            }

            // Handle profile picture upload
            if (model.ProfilePictureFile != null)
            {
                try
                {
                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                    {
                        await _fileUploadService.DeleteImageAsync(user.ProfilePicture);
                    }

                    // Upload new profile picture
                    var profilePictureUrl = await _fileUploadService.UploadImageAsync(
                        model.ProfilePictureFile, "profiles");
                    user.ProfilePicture = profilePictureUrl;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("ProfilePictureFile", "Có lỗi xảy ra khi tải ảnh lên. Vui lòng thử lại.");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }
            }            // Update user information
            user.FirstName = model.FirstName ?? string.Empty;
            user.LastName = model.LastName ?? string.Empty;
            user.PhoneNumber = model.PhoneNumber ?? string.Empty;
            user.Address = model.Address;
            user.Bio = model.Bio;
            user.UpdatedAt = DateTime.UtcNow;

            // Update email if changed
            if (model.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Thông tin cá nhân đã được cập nhật thành công!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.CurrentProfilePicture = user.ProfilePicture;
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Mật khẩu đã được thay đổi thành công!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }        private IActionResult LoginWith2fa(string? returnUrl = null, bool rememberMe = false)
        {
            // Implement two-factor authentication if needed
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            // Request a redirect to the external login provider
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["Error"] = $"Lỗi từ nhà cung cấp bên ngoài: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["Error"] = "Không thể lấy thông tin từ nhà cung cấp bên ngoài.";
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                TempData["Error"] = "Tài khoản đã bị khóa.";
                return RedirectToAction(nameof(Login));
            }

            // If the user does not have an account, then ask the user to create an account
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Không thể lấy email từ nhà cung cấp bên ngoài.";
                return RedirectToAction(nameof(Login));
            }

            // Check if user already exists with this email
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Add this external login to the existing user
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    TempData["Error"] = "Không thể liên kết tài khoản với nhà cung cấp bên ngoài.";
                    return RedirectToAction(nameof(Login));
                }
            }

            // Create a new user
            var user = new WebHSUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true // External logins are assumed to have confirmed emails
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                // Add user to default role
                await _userManager.AddToRoleAsync(user, WebHS.Models.UserRoles.User);

                // Add the external login
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
            }

            // If we got this far, something failed
            TempData["Error"] = "Không thể tạo tài khoản từ nhà cung cấp bên ngoài.";
            return RedirectToAction(nameof(Login));
        }
    }
}

