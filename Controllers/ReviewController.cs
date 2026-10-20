using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Assignment742.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class ReviewController :  Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    [HttpPost]
    [Authorize(Roles = "Guest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(SubmitReviewViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        if (!ModelState.IsValid)
            return BadRequest("Invalid review details.");

        var property = await _context.Bookings
            .Where(b => b.Id == model.BookingId &&
                   b.GuestId == user.Id &&
                   b.Status == BookingStatus.Approved)
            .Select(p  => p.Property)
            .FirstOrDefaultAsync();

        if (property == null)
            return NotFound();

        var hasCheckOutStatus = await _context.GuestCheckIns
            .AnyAsync(c => c.BookingId == model.BookingId &&
                                       c.Status == CheckInStatus.CheckedOut);

        if (!hasCheckOutStatus)
            return BadRequest("Reviews can only be left after checkout");
        
        var review = new Review
        {
            ReviewerId = user.Id,
            PropertyId = property.Id,
            Rating = model.Rating,
            Comment = model.Comment.Trim()
        };

        var notification = new Notification
        {
            UserId = property.HostId,
            Title = $"{user.FirstName} {user.LastName} left a review on one of your properties",
            Type = NotificationType.Review,
            Status = NotificationStatus.Unread
        };

        var activity = new ActivityLog
        {
            UserId = property.HostId,
            Action =  "Review",
            ActivityType =  ActivityType.Create,
        };

        _context.Notifications.Add(notification);
        _context.ActivityLogs.Add(activity);
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Success: Your review has been submitted";

        return RedirectToAction("Index", "Booking");
    }
    
    [Authorize(Roles = "Host")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var host = await _userManager.GetUserAsync(User);
        
        if (host == null)
            return Challenge();
        
        var reviews = await _context.Reviews
            .Where(r => r.Property.HostId == host.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new HostPropertyReviewViewModel
            {
                PropertyTitle = r.Property.Title,
                Reviewer = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
        
         var averageRating = await _context.Reviews
            .Where(r => r.Property.HostId == host.Id)
            .Select(r => (double?)r.Rating)
            .AverageAsync() ?? 0;
         
         ViewBag.AverageRating =  averageRating;
         
        return View(reviews);
    }
}

    
