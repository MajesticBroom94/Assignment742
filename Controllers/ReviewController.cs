using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Assignment742.Models;
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(SubmitReviewViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        if (!ModelState.IsValid)
            return BadRequest("Invalid review details.");

        var propertyId = await _context.Bookings
            .Where(b =>
                b.Id == model.BookingId &&
                b.GuestId == user.Id &&
                b.Status == BookingStatus.Approved)
            .Select(b => (int?)b.PropertyId)
            .FirstOrDefaultAsync();

        if (propertyId == null)
            return NotFound();

        var hasCheckOutStatus = await _context.GuestCheckIns
            .AnyAsync(c => c.BookingId == model.BookingId &&
                                       c.Status == CheckInStatus.CheckedOut);

        if (!hasCheckOutStatus)
            return BadRequest("Reviews can only be left after checkout");
        
        var review = new Review
        {
            ReviewerId = user.Id,
            PropertyId = propertyId.Value,
            Rating = model.Rating,
            Comment = model.Comment.Trim()
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok();
    }

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

    
