using MailKit.Net.Smtp;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSUser = WebHS.Models.User;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace WebHS.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = true;
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendConfirmationEmailAsync(string to, string confirmationLink);
        Task SendResetPasswordEmailAsync(string to, string resetLink);
        Task SendBookingConfirmationAsync(string to, string guestName, string homestayName, DateTime checkIn, DateTime checkOut);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _hostEnvironment;

        public EmailService(IOptions<EmailSettings> emailSettings, IWebHostEnvironment hostEnvironment)
        {
            _emailSettings = emailSettings.Value;
            _hostEnvironment = hostEnvironment;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            // Skip email sending in development environment
            if (_hostEnvironment.IsDevelopment())
            {
                // Just return without sending email in development
                Console.WriteLine($"[DEV MODE] Email would be sent to: {to}");
                Console.WriteLine($"[DEV MODE] Subject: {subject}");
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                // Fix SMTP connection for port 587 (use STARTTLS) vs 465 (use SSL)
                SecureSocketOptions secureOptions;
                if (_emailSettings.SmtpPort == 465)
                {
                    secureOptions = SecureSocketOptions.SslOnConnect;
                }
                else if (_emailSettings.SmtpPort == 587)
                {
                    secureOptions = SecureSocketOptions.StartTls;
                }
                else
                {
                    secureOptions = _emailSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                }
                
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, secureOptions);
                
                // Only authenticate if credentials are provided
                if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername) && !string.IsNullOrEmpty(_emailSettings.SmtpPassword))
                {
                    await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                }
                
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // For production, you should log this error properly
                Console.WriteLine($"Email sending failed: {ex.Message}");
                // throw; // Uncomment for production debugging
            }
        }

        public async Task SendConfirmationEmailAsync(string to, string confirmationLink)
        {
            var subject = "Xác nhận tài khoản - HomestayBooking";
            var body = $@"
                <h2>Chào mừng bạn đến với HomestayBooking!</h2>
                <p>Vui lòng click vào link dưới đây để xác nhận tài khoản:</p>
                <p><a href='{confirmationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Xác nhận tài khoản</a></p>
                <p>Nếu bạn không thể click vào button, hãy copy link sau vào trình duyệt:</p>
                <p>{confirmationLink}</p>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendResetPasswordEmailAsync(string to, string resetLink)
        {
            var subject = "Đặt lại mật khẩu - HomestayBooking";
            var body = $@"
                <h2>Đặt lại mật khẩu</h2>
                <p>Bạn đã yêu cầu đặt lại mật khẩu. Click vào link dưới đây để đặt lại:</p>
                <p><a href='{resetLink}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Đặt lại mật khẩu</a></p>
                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
                <p>Link này sẽ hết hạn sau 24 giờ.</p>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendBookingConfirmationAsync(string to, string guestName, string homestayName, DateTime checkIn, DateTime checkOut)
        {
            var subject = "Xác nhận đặt phòng thành công - HomestayBooking";
            var body = $@"
                <h2>Xác nhận đặt phòng thành công!</h2>
                <p>Chào {guestName},</p>
                <p>Đặt phòng của bạn đã được xác nhận thành công!</p>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                    <h3>Chi tiết đặt phòng:</h3>
                    <p><strong>Homestay:</strong> {homestayName}</p>
                    <p><strong>Ngày nhận phòng:</strong> {checkIn:dd/MM/yyyy}</p>
                    <p><strong>Ngày trả phòng:</strong> {checkOut:dd/MM/yyyy}</p>
                </div>
                <p>Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ của chúng tôi!</p>
            ";

            await SendEmailAsync(to, subject, body);
        }
    }
}

