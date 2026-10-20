using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WishlistController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<WishlistController> logger)
    {
        _context = context;
        _userManager = userManager;
    }

    
    [HttpPost]
    public async Task<IActionResult> HandleStatus([FromBody] int propertyId)
    {
        var guest = await _userManager.GetUserAsync(User);

        if (guest == null)
        {
            return Json(new { 
                success = false,
                message = "Please login to wishlist properties" 
            });
        }
        
        var property = await _context.HostProperties
            .Where(p => p.Id == propertyId && p.Status == PropertyStatus.Active)
            .FirstOrDefaultAsync();
        
        if (property == null)
        {
            return Json(new { 
                success = false,
                message = "This property is not available for wishlisting" 
            });
        }
        
        var wishlistItem = await  _context.WishlistItems
            .Where(p => p.PropertyId == propertyId && p.UserId == guest.Id)
            .FirstOrDefaultAsync();

        if (wishlistItem == null)
        {
            _context.WishlistItems.Add(new WishlistItem {PropertyId = propertyId, UserId = guest.Id});
            await _context.SaveChangesAsync();
            return Json(new { 
                success = true,
                message = "added" 
            });
        }
        
        _context.WishlistItems.Remove(wishlistItem);
        await _context.SaveChangesAsync();
        return Json(new { 
            success = true,
            message = "removed" 
        });
    }
}