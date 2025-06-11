using System.ComponentModel.DataAnnotations;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Models
{
    public class UserNotification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Type { get; set; } = "info"; // info, success, warning, danger
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual WebHSUser User { get; set; } = null!;
    }
}
