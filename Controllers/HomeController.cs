using System.Diagnostics;
using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Assignment742.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class HomeController : Controller
{
    
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        
        var model = await _context.HostProperties
            .Where(p => p.Status == PropertyStatus.Active)
            .Select(p => new PropertyGuestViewModel
                {
                    Id = p.Id,
                    Address = p.Address,
                    Title =  p.Title,
                    PricePerNight =   p.PricePerNight,
                    ImageUrl = p.PropertyImages
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault() ?? "",
                    ReviewCount = p.Reviews.Count,
                    Rating =  p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0.0,
                    IsFavorite = user != null && p.WishlistItems.Any(w => w.UserId == user.Id)
                }
            ).ToListAsync();
        
        return View(model);
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}