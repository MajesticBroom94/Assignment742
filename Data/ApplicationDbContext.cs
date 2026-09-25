using Assignment742.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
    
    public DbSet<ActivityLog> ActivityLogs {get; set;}
    public DbSet<Amenity> Amenities {get; set;}
    public DbSet<Booking> Bookings {get; set;}
    public DbSet<CheckInProcess> CheckInProcesses {get; set;}
    public DbSet<GuestCheckIn> GuestCheckIns {get; set;}
    public DbSet<GuestDocument> GuestDocuments {get; set;}
    public DbSet<HostApplication> HostApplications {get; set;}
    public DbSet<HostProperty> HostProperties {get; set;}
    public DbSet<Message> Messages {get; set;}
    public DbSet<Notification> Notifications {get; set;}
    public DbSet<Payment> Payments {get; set;}
    public DbSet<PropertyImage> PropertyImages {get; set;}
    public DbSet<Review> Reviews {get; set;}
    public DbSet<WishlistItem> WishlistItems {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // ApplicationUser -> Booking
    modelBuilder.Entity<Booking>()
        .HasOne(b => b.Guest)
        .WithMany(u => u.Bookings)
        .HasForeignKey(b => b.GuestId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // ApplicationUser -> HostProperty
    modelBuilder.Entity<HostProperty>()
        .HasOne(p => p.Host)
        .WithMany(u => u.HostProperties)
        .HasForeignKey(p => p.HostId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // HostProperty -> Booking
    modelBuilder.Entity<Booking>()
        .HasOne(b => b.Property)
        .WithMany(p => p.Bookings)
        .HasForeignKey(b => b.PropertyId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // HostProperty -> Review
    modelBuilder.Entity<Review>()
        .HasOne(r => r.Property)
        .WithMany(p => p.Reviews)
        .HasForeignKey(r => r.PropertyId)
        .OnDelete(DeleteBehavior.Cascade);

    // ApplicationUser -> Review
    modelBuilder.Entity<Review>()
        .HasOne(r => r.Reviewer)
        .WithMany(u => u.Reviews)
        .HasForeignKey(r => r.ReviewerId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // ApplicationUser -> WishlistItem
    modelBuilder.Entity<WishlistItem>()
        .HasOne(w => w.User)
        .WithMany(u => u.WishlistItems)
        .HasForeignKey(w => w.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // HostProperty -> WishlistItem
    modelBuilder.Entity<WishlistItem>()
        .HasOne(w => w.Property)
        .WithMany(p => p.WishlistItems)
        .HasForeignKey(w => w.PropertyId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Prevent duplicate wishlist entries
    modelBuilder.Entity<WishlistItem>()
        .HasIndex(w => new
        {
            w.UserId,
            w.PropertyId
        })
        .IsUnique();
    
    // HostProperty -> PropertyImage
    modelBuilder.Entity<PropertyImage>()
        .HasOne(i => i.Property)
        .WithMany(p => p.PropertyImages)
        .HasForeignKey(i => i.PropertyId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // HostProperty -> CheckInProcess
    modelBuilder.Entity<CheckInProcess>()
        .HasOne(c => c.Property)
        .WithOne(p => p.CheckInProcess)
        .HasForeignKey<CheckInProcess>(c => c.PropertyId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Ensure one CheckInProcess per property
    modelBuilder.Entity<CheckInProcess>()
        .HasIndex(c => c.PropertyId)
        .IsUnique();
    
    // Booking -> GuestCheckIn
    modelBuilder.Entity<GuestCheckIn>()
        .HasOne(g => g.Booking)
        .WithOne(b => b.GuestCheckIn)
        .HasForeignKey<GuestCheckIn>(g => g.BookingId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Ensure one check-in per booking
    modelBuilder.Entity<GuestCheckIn>()
        .HasIndex(g => g.BookingId)
        .IsUnique();
    
    // CheckInProcess -> GuestCheckIn
    modelBuilder.Entity<GuestCheckIn>()
        .HasOne(g => g.CheckInProcess)
        .WithMany(c => c.GuestCheckIns)
        .HasForeignKey(g => g.CheckInProcessId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // GuestCheckIn -> GuestDocument
    modelBuilder.Entity<GuestDocument>()
        .HasOne(d => d.GuestCheckIn)
        .WithOne(g => g.GuestDocument)
        .HasForeignKey<GuestDocument>(d => d.GuestCheckInId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Ensure one document per check-in
    modelBuilder.Entity<GuestDocument>()
        .HasIndex(d => d.GuestCheckInId)
        .IsUnique();
    
    // Booking -> Payment
    modelBuilder.Entity<Payment>()
        .HasOne(p => p.Booking)
        .WithOne(b => b.Payment)
        .HasForeignKey<Payment>(p => p.BookingId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Ensure one payment per booking
    modelBuilder.Entity<Payment>()
        .HasIndex(p => p.BookingId)
        .IsUnique();
    
    // ApplicationUser -> Notification
    modelBuilder.Entity<Notification>()
        .HasOne(n => n.User)
        .WithMany(u => u.Notifications)
        .HasForeignKey(n => n.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // ApplicationUser -> ActivityLog
    modelBuilder.Entity<ActivityLog>()
        .HasOne(a => a.User)
        .WithMany(u => u.ActivityLogs)
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // ApplicationUser -> Message Sender
    modelBuilder.Entity<Message>()
        .HasOne(m => m.Sender)
        .WithMany(u => u.SentMessages)
        .HasForeignKey(m => m.SenderId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // ApplicationUser -> MessageReceiver
    modelBuilder.Entity<Message>()
        .HasOne(m => m.Receiver)
        .WithMany(u => u.ReceivedMessages)
        .HasForeignKey(m => m.ReceiverId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // ApplicationUser -> HostApplication
    modelBuilder.Entity<HostApplication>()
        .HasOne(a => a.ApplicationUser)
        .WithMany(u => u.HostApplications)
        .HasForeignKey(a => a.ApplicationUserId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // HostProperty -> HostApplication
    modelBuilder.Entity<HostApplication>()
        .HasOne(a => a.Property)
        .WithMany(p => p.HostApplications)
        .HasForeignKey(a => a.PropertyId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // Property <-> Amenity
    modelBuilder.Entity<HostProperty>()
        .HasMany(p => p.Amenities)
        .WithMany(a => a.HostProperties);
    
    // Decimal precision
    modelBuilder.Entity<HostProperty>()
        .Property(p => p.PricePerNight)
        .HasPrecision(18, 2);

    modelBuilder.Entity<Booking>()
        .Property(b => b.TotalPrice)
        .HasPrecision(18, 2);

    modelBuilder.Entity<Payment>()
        .Property(p => p.Amount)
        .HasPrecision(18, 2);
    }
}