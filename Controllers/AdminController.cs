using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Assignment742.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class AdminController : Controller
{
    
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    
    public AdminController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }
    [HttpGet]
    
    public async Task<IActionResult> ManageUsers(string searchButton, string? role, string? search)
    {
        if (searchButton == "clear")
        {
            role = null;
            search = null;
        }
        
        var allowedRoles = await _roleManager.Roles
            .Select(r => r.Name!)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(role) &&
            !allowedRoles.Contains(role))
            return BadRequest("Invalid role filter.");

        search = search?.Trim();

        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)));
        }

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        var model = new List<UserManagementViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "No Role";

            if (!string.IsNullOrEmpty(role) && userRole != role)
                continue;

            model.Add(new UserManagementViewModel
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? "",
                Role = userRole,
                ProfilePictureUrl = user.ProfilePictureUrl
                                    ?? "/Icons/profile.png",
            });
        }

        ViewBag.AvailableRoles = allowedRoles;
        ViewBag.SelectedRole = role;
        ViewBag.Search = search;

        return View(model);
    }
    
    
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Count != 1 || roles[0] != "Guest")
        {
            TempData["Error"] = "Only Guests can be promoted to Admin.";
            return RedirectToAction(nameof(ManageUsers));
        }
        
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var removeResult = await _userManager.RemoveFromRoleAsync(user, "Guest");

        if (!removeResult.Succeeded)
        {
            TempData["Error"] = "Failed to remove Guest role.";
            return RedirectToAction(nameof(ManageUsers));
        }

        var addResult = await _userManager.AddToRoleAsync(user, "Admin");

        if (!addResult.Succeeded)
        {
            TempData["Error"] = "Failed to assign Admin role.";
            return RedirectToAction(nameof(ManageUsers));
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);

        if (!stampResult.Succeeded)
        {
            TempData["Error"] = "Failed to update user session.";
            return RedirectToAction(nameof(ManageUsers));
        }

        await transaction.CommitAsync();

        TempData["Success"] = "Guest promoted to Admin successfully.";

        return RedirectToAction(nameof(ManageUsers));
    }    
    
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Dashboard()
    {
        var hosts = await _userManager.GetUsersInRoleAsync("Host");
        
        var model = new AdminDashboardViewModel
        {
            TotalUsers = await _userManager.Users.CountAsync(),

            PendingHostApplications = await _context.HostApplications
                .CountAsync(a => a.Status == ApplicationStatus.Pending),

            ActiveProperties = await _context.HostProperties
                .CountAsync(p => p.Status == PropertyStatus.Active),
            
            PlatformRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount),
            
            ApprovedHosts = hosts.Count,

            TotalBookings = await _context.Bookings
                .CountAsync(),

            PendingBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Pending),
            
            RecentActivities = await _context.ActivityLogs.Select(a => new DashboardActivityViewModel
            {
                UserName = a.User != null ? a.User.FirstName + " " + a.User.LastName : "Unknown User",
                UserEmail = a.User != null ? a.User.Email ?? "" : "",
                Action =  a.Action,
                Date =  a.CreatedAt,
                Type = a.ActivityType
            })
                .OrderByDescending(o => o.Date)
                .Take(4)
                .ToListAsync(),
            
            RecentApplications = await _context.HostApplications.Select(a => new DashboardHostApplicationViewModel
            {
                ApplicationId = a.Id,
                ApplicantName = a.ApplicationUser.FirstName + " " + a.ApplicationUser.LastName,
                ApplicantEmail = a.ApplicationUser.Email ?? "",
                AppliedAt = a.AppliedAt,
                Status = a.Status
            })
                .OrderByDescending(o => o.AppliedAt)
                .Take(4)
                .ToListAsync(),
            
            UpcomingBookings = await _context.Bookings
                .Where(b =>
                    b.CheckInDate >= DateTime.Today &&
                    b.Status == BookingStatus.Approved)
                .OrderBy(b => b.CheckInDate)
                .Take(4)
                .Select(b => new DashboardBookingViewModel
                {
                    BookingId = b.Id,
                    GuestName = b.Guest.FirstName + " " + b.Guest.LastName,
                    GuestEmail = b.Guest.Email ?? "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status
                })
                .ToListAsync(),
        };
        
        return View(model);
    }
}
