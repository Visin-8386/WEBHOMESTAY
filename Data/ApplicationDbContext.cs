using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebHS.Models;
using WebHS.Services;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.Data
{
    public class ApplicationDbContext : IdentityDbContext<WebHSUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Homestay> Homestays { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<HomestayAmenity> HomestayAmenities { get; set; }
        public DbSet<HomestayImage> HomestayImages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<WebHSPromotion> Promotions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<BlockedDate> BlockedDates { get; set; }
        public DbSet<HomestayPricing> HomestayPricings { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure HomestayAmenity many-to-many relationship
            modelBuilder.Entity<HomestayAmenity>()
                .HasKey(ha => new { ha.HomestayId, ha.AmenityId });

            modelBuilder.Entity<HomestayAmenity>()
                .HasOne(ha => ha.Homestay)
                .WithMany(h => h.HomestayAmenities)
                .HasForeignKey(ha => ha.HomestayId);

            modelBuilder.Entity<HomestayAmenity>()
                .HasOne(ha => ha.Amenity)
                .WithMany(a => a.HomestayAmenities)
                .HasForeignKey(ha => ha.AmenityId);

            // Configure User-Homestay relationship (Host)
            modelBuilder.Entity<Homestay>()
                .HasOne(h => h.Host)
                .WithMany(u => u.Homestays)
                .HasForeignKey(h => h.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User-Booking relationship with explicit property mapping
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .Property(b => b.UserId)
                .HasColumnName("UserId");

            // Configure User-Payment relationship with explicit property mapping
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .Property(p => p.UserId)
                .HasColumnName("UserId");

            // Configure Booking-Promotion relationship
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Promotion)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PromotionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Promotion-User relationship
            modelBuilder.Entity<WebHSPromotion>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Explicitly configure User.Id as primary key
            modelBuilder.Entity<WebHSUser>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<WebHSUser>()
                .Property(u => u.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            // Configure decimal precision
            modelBuilder.Entity<Homestay>()
                .Property(h => h.Latitude)
                .HasColumnType("decimal(10,8)");

            modelBuilder.Entity<Homestay>()
                .Property(h => h.Longitude)
                .HasColumnType("decimal(11,8)");

            // Configure indexes
            modelBuilder.Entity<Homestay>()
                .HasIndex(h => h.City);

            modelBuilder.Entity<Homestay>()
                .HasIndex(h => h.IsActive);

            modelBuilder.Entity<Homestay>()
                .HasIndex(h => h.IsApproved);

            modelBuilder.Entity<WebHSPromotion>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<BlockedDate>()
                .HasIndex(bd => new { bd.HomestayId, bd.Date })
                .IsUnique();

            // Configure HomestayPricing relationship
            modelBuilder.Entity<HomestayPricing>()
                .HasOne(hp => hp.Homestay)
                .WithMany(h => h.PricingRules)
                .HasForeignKey(hp => hp.HomestayId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HomestayPricing>()
                .HasIndex(hp => new { hp.HomestayId, hp.Date })
                .IsUnique();

            modelBuilder.Entity<HomestayPricing>()
                .Property(hp => hp.PricePerNight)
                .HasColumnType("decimal(10,2)");

            // Configure Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Homestay)
                .WithMany()
                .HasForeignKey(m => m.HomestayId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Booking)
                .WithMany()
                .HasForeignKey(m => m.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Conversation relationships
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.LastMessageSender)
                .WithMany()
                .HasForeignKey(c => c.LastMessageSenderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Homestay)
                .WithMany()
                .HasForeignKey(c => c.HomestayId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Message-Conversation relationship
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for Message
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId });

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.SentAt);

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.IsRead);

            // Add indexes for Conversation
            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique();

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.LastMessageAt);
        }
    }
}
