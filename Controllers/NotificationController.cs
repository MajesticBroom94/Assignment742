using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeNotification(int notificationId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == notificationId &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        if (notification.Status == NotificationStatus.Unread)
        {
            notification.Status = NotificationStatus.Read;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideNotification(
        int notificationId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == notificationId &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        notification.Status = NotificationStatus.Dismissed;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> NotificationCount()
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return StatusCode(404, new { message = "User not found" });
        
        var count = await  _context.Notifications
            .CountAsync(n => n.UserId == user.Id &&
                                       n.Status == NotificationStatus.Unread);
        
        return Json(new {count});
    }
    

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }
        
        var notifications = await _context.Notifications
            .Where(n => n.UserId == user.Id && n.Status != NotificationStatus.Dismissed)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        
        return View(notifications);
    }
    
    
}