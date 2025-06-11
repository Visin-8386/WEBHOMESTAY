using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using WebHS.Models;
using WebHS.Services;
using WebHS.ViewModels;
using System.Security.Claims;

namespace WebHS.Controllers
{
    [Authorize]
    public class MessagingController : Controller
    {
        private readonly IMessagingService _messagingService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<MessagingController> _logger;

        public MessagingController(
            IMessagingService messagingService,
            UserManager<User> userManager,
            ILogger<MessagingController> logger)
        {
            _messagingService = messagingService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? conversationId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            var conversations = await _messagingService.GetUserConversationsAsync(currentUserId);
            var unreadCount = await _messagingService.GetUnreadMessageCountAsync(currentUserId);
            
            var viewModel = new MessagingIndexViewModel
            {
                Conversations = conversations,
                CurrentUserId = currentUserId,
                CurrentUserName = User.Identity?.Name ?? "",
                HasUnreadMessages = unreadCount > 0,
                TotalUnreadCount = unreadCount
            };

            // If specific conversation is requested
            if (conversationId.HasValue)
            {
                viewModel.ActiveConversation = await _messagingService.GetConversationAsync(conversationId.Value, currentUserId);
                if (viewModel.ActiveConversation != null)
                {
                    viewModel.Messages = await _messagingService.GetConversationMessagesAsync(conversationId.Value, currentUserId);
                    // Mark conversation as read
                    await _messagingService.MarkConversationAsReadAsync(conversationId.Value, currentUserId);
                }
            }
            else if (conversations.Any())
            {
                // Select first conversation if none specified
                var firstConversation = conversations.First();
                viewModel.ActiveConversation = firstConversation;
                viewModel.Messages = await _messagingService.GetConversationMessagesAsync(firstConversation.Id, currentUserId);
                await _messagingService.MarkConversationAsReadAsync(firstConversation.Id, currentUserId);
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(int id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var conversation = await _messagingService.GetConversationAsync(id, currentUserId);
            if (conversation == null)
                return Json(new { success = false, message = "Conversation not found" });

            var messages = await _messagingService.GetConversationMessagesAsync(id, currentUserId);
            await _messagingService.MarkConversationAsReadAsync(id, currentUserId);

            return Json(new { 
                success = true, 
                conversation = conversation,
                messages = messages 
            });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageViewModel model)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrWhiteSpace(model.Content))
                return Json(new { success = false, message = "Message content is required" });

            var message = await _messagingService.SendMessageAsync(currentUserId, model);
            if (message == null)
                return Json(new { success = false, message = "Failed to send message" });

            return Json(new { success = true, message = message });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessageWithFile(SendMessageViewModel model)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var message = await _messagingService.SendMessageAsync(currentUserId, model);
            if (message == null)
                return Json(new { success = false, message = "Failed to send message" });

            return Json(new { success = true, message = message });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _messagingService.MarkMessageAsReadAsync(messageId, currentUserId);
            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrWhiteSpace(term))
                return Json(new { success = true, users = new List<UserSearchResultViewModel>() });

            var users = await _messagingService.SearchUsersAsync(term, currentUserId);
            return Json(new { success = true, users = users });
        }

        [HttpGet]
        public async Task<IActionResult> StartConversation(string withUserId, int? homestayId = null, int? bookingId = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            var canMessage = await _messagingService.CanUserMessageAsync(currentUserId, withUserId);
            if (!canMessage)
            {
                TempData["Error"] = "Bạn không có quyền nhắn tin với người dùng này.";
                return RedirectToAction("Index");
            }

            var conversation = await _messagingService.GetOrCreateConversationAsync(currentUserId, withUserId, homestayId, bookingId);
            if (conversation == null)
            {
                TempData["Error"] = "Không thể tạo cuộc trò chuyện.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index", new { conversationId = conversation.Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _messagingService.DeleteMessageAsync(messageId, currentUserId);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveConversation(int conversationId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _messagingService.ArchiveConversationAsync(conversationId, currentUserId);
            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, count = 0 });

            var count = await _messagingService.GetUnreadMessageCountAsync(currentUserId);
            return Json(new { success = true, count = count });
        }

        // API endpoints for AJAX calls
        [HttpGet]
        public async Task<IActionResult> LoadMoreMessages(int conversationId, int page = 1)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var messages = await _messagingService.GetConversationMessagesAsync(conversationId, currentUserId, page);
            return Json(new { success = true, messages = messages });
        }

        [HttpPost]
        public async Task<IActionResult> RequestAdminMessage([FromBody] AdminMessageRequestModel model)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrEmpty(model.Message))
                return Json(new { success = false, message = "Message cannot be empty" });

            var result = await _messagingService.RequestAdminMessageAsync(currentUserId, model.Message);
            
            if (result)
                return Json(new { success = true, message = "Yêu cầu nhắn tin đã được gửi tới Admin" });
            else
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi yêu cầu" });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminMessageRequests()
        {
            var requests = await _messagingService.GetAdminMessageRequestsAsync();
            return Json(new { success = true, requests = requests });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AcceptAdminMessageRequest(int notificationId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _messagingService.AcceptAdminMessageRequestAsync(notificationId, currentUserId);
            
            if (result)
                return Json(new { success = true, message = "Đã chấp nhận yêu cầu nhắn tin" });
            else
                return Json(new { success = false, message = "Có lỗi xảy ra" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeclineAdminMessageRequest(int notificationId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var result = await _messagingService.DeclineAdminMessageRequestAsync(notificationId, currentUserId);
            
            if (result)
                return Json(new { success = true, message = "Đã từ chối yêu cầu nhắn tin" });
            else
                return Json(new { success = false, message = "Có lỗi xảy ra" });
        }

        [HttpPost]
        public async Task<IActionResult> StartHostConversation([FromBody] HostConversationModel model)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Json(new { success = false, message = "User not authenticated" });

            var conversation = await _messagingService.GetOrCreateHostConversationAsync(currentUserId, model.HostId, model.HomestayId);
            
            if (conversation != null)
                return Json(new { success = true, conversationId = conversation.Id, message = "Đã tạo cuộc trò chuyện" });
            else
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo cuộc trò chuyện" });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminRequests()
        {
            return View();
        }
    }

    public class AdminMessageRequestModel
    {
        public string Message { get; set; } = string.Empty;
    }

    public class HostConversationModel
    {
        public string HostId { get; set; } = string.Empty;
        public int HomestayId { get; set; }
    }
}
