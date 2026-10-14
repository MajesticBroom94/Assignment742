using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Assignment742.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class HostController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HostController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> HostApplication(
        int? filterId = null, 
        ApplicationStatus status = ApplicationStatus.Pending)
    {
        var query = _context.HostApplications.AsQueryable();

        if (filterId.HasValue)
        {
            query = query.Where(a => a.Id == filterId.Value);
        }
        else
        {
            query = query.Where(a => a.Status == status);
        }

        var applications = await query
            .OrderBy(a => a.AppliedAt)
            .Select(a => new HostApplicationViewModel
            {
                ApplicationId = a.Id,
                PropertyId = a.PropertyId,

                PropertyTitle = a.Property.Title,
                PropertyAddress = a.Property.Address,

                HostId = a.ApplicationUserId,
                HostName = a.ApplicationUser.FirstName + " " +
                           a.ApplicationUser.LastName,
                HostEmail = a.ApplicationUser.Email ?? "",

                AppliedAt = a.AppliedAt,
                Status = a.Status,

                ImageUrl = a.Property.PropertyImages
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync();

        ViewBag.SelectedStatus = status;
        
        return View(applications);
    }


    
    
   
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessHostApplication(
        int applicationId,
        ApplicationStatus status)
    {
        if (status is not
            (ApplicationStatus.Approved or ApplicationStatus.Rejected))
        {
            return BadRequest("Invalid application status.");
        }

        var application = await _context.HostApplications
            .Include(a => a.Property)
            .Include(a => a.ApplicationUser)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            return NotFound();

        if (application.Status != ApplicationStatus.Pending ||
            application.Property.Status != PropertyStatus.AwaitingApproval)
        {
            TempData["Error"] = "This application has already been processed.";
            return RedirectToAction(nameof(HostApplication));
        }

        if (application.Property.HostId != application.ApplicationUserId)
            return BadRequest("Application and property owner do not match.");

        var applicant = application.ApplicationUser;

        var roles = await _userManager.GetRolesAsync(applicant);

        if (roles.Count != 1 || roles[0] != "Guest")
        {
            TempData["Error"] = "Only Guests can have a host application processed.";
            return RedirectToAction(nameof(HostApplication));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (status == ApplicationStatus.Approved)
            {
                // Remove Guest role.
                var removeResult = await _userManager.RemoveFromRoleAsync(applicant, "Guest");

                if (!removeResult.Succeeded)
                    throw new InvalidOperationException(
                        "Could not remove Guest role.");

                // Assign Host role.
                var addResult =
                    await _userManager.AddToRoleAsync(applicant, "Host");

                if (!addResult.Succeeded)
                    throw new InvalidOperationException(
                        "Could not assign Host role.");

                // Invalidate existing authentication sessions.
                var stampResult = await _userManager.UpdateSecurityStampAsync(applicant);

                if (!stampResult.Succeeded)
                    throw new InvalidOperationException(
                        "Could not invalidate user sessions.");

                application.Status = ApplicationStatus.Approved;
                application.Property.Status = PropertyStatus.Active;
            }
            else
            {
                // Rejection leaves the applicant as a Guest.
                application.Status = ApplicationStatus.Rejected;
                application.Property.Status = PropertyStatus.Rejected;
            }

            // Save application and property changes.
            await _context.SaveChangesAsync();

            // Commit everything together.
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            TempData["Error"] = "The application could not be processed.";

            return RedirectToAction(nameof(HostApplication));
        }

        TempData["Success"] = status == ApplicationStatus.Approved
            ? "Application approved. User has been promoted to Host."
            : "Host application rejected.";

        return RedirectToAction(nameof(HostApplication));
    }
    
    [HttpGet]
    [Authorize(Roles = "Host")]
    public async Task<IActionResult> Dashboard()
    {
         var user = await _userManager.GetUserAsync(User);

    if (user == null)
        return Challenge();

    var monthStart = new DateTime(
        DateTime.Today.Year,
        DateTime.Today.Month,
        1);

    var nextMonthStart = monthStart.AddMonths(1);

    var model = new HostDashboardViewModel
    {
        HostName = user.FirstName + " " + user.LastName,

        TotalProperties = await _context.HostProperties
            .CountAsync(p => p.HostId == user.Id),

        ActiveProperties = await _context.HostProperties
            .CountAsync(p =>
                p.HostId == user.Id &&
                p.Status == PropertyStatus.Active),

        TotalBookings = await _context.Bookings
            .CountAsync(b => b.Property.HostId == user.Id),

        MonthlyBookings = await _context.Bookings
            .CountAsync(b =>
                b.Property.HostId == user.Id &&
                b.CheckInDate >= monthStart &&
                b.CheckInDate < nextMonthStart),

        PendingBookings = await _context.Bookings
            .CountAsync(b =>
                b.Property.HostId == user.Id &&
                b.Status == BookingStatus.Pending),

        TotalEarnings = await _context.Bookings
            .Where(b =>
                b.Property.HostId == user.Id &&
                b.Payment != null &&
                b.Payment.Status == PaymentStatus.Completed)
            .SumAsync(b => b.TotalPrice),

        MonthlyEarnings = await _context.Bookings
            .Where(b =>
                b.Property.HostId == user.Id &&
                b.Payment != null &&
                b.Payment.Status == PaymentStatus.Completed &&
                b.Payment.CreatedAt >= monthStart &&
                b.Payment.CreatedAt < nextMonthStart)
            .SumAsync(b => b.TotalPrice),

        AverageRating = await _context.Reviews
            .Where(r => r.Property.HostId == user.Id)
            .Select(r => (double?)r.Rating)
            .AverageAsync() ?? 0,

        TotalReviews = await _context.Reviews
            .CountAsync(r => r.Property.HostId == user.Id)
    };

    return View(model);
    }
    
    
    [HttpGet]
    public async Task<IActionResult> Earnings()
    {
        var host = await _userManager.GetUserAsync(User);

        if (host == null)
            return Challenge();

        var now = DateTime.UtcNow;

        var monthStart = new DateTime(
            now.Year, now.Month, 1, 0, 0, 0,
            DateTimeKind.Utc);

        var nextMonthStart = monthStart.AddMonths(1);
        var historyStart = monthStart.AddMonths(-5);

        var payments = _context.Payments
            .Where(p =>
                p.Booking.Property.HostId == host.Id &&
                p.Status == PaymentStatus.Completed);

        // Last six months, grouped by month
        var monthlyData = await payments
            .Where(p =>
                p.CreatedAt >= historyStart &&
                p.CreatedAt < nextMonthStart)
            .GroupBy(p => new
            {
                p.CreatedAt.Year,
                p.CreatedAt.Month
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(p => p.Amount),
                Count = g.Count()
            })
            .ToListAsync();

        var model = new HostEarningsViewModel
        {
            TotalEarnings = await payments
                .SumAsync(p => p.Amount),

            MonthlyEarnings = await payments
                .Where(p =>
                    p.CreatedAt >= monthStart &&
                    p.CreatedAt < nextMonthStart)
                .SumAsync(p => p.Amount),

            CompletedPayments = await payments.CountAsync(),

            RecentPayments = await payments
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .Select(p => new HostPaymentViewModel
                {
                    BookingId = p.BookingId,
                    PropertyTitle = p.Booking.Property.Title,
                    GuestName =
                        p.Booking.Guest.FirstName + " " +
                        p.Booking.Guest.LastName,
                    Amount = p.Amount,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync()
        };

        // Fill missing months with zero earnings.
        for (int i = 0; i < 6; i++)
        {
            var month = historyStart.AddMonths(i);

            var data = monthlyData.FirstOrDefault(m =>
                m.Year == month.Year &&
                m.Month == month.Month);

            model.MonthlyEarningsHistory.Add(
                new MonthlyEarningViewModel
                {
                    Month = month.ToString("MMM yyyy"),
                    Amount = data?.Amount ?? 0,
                    PaymentCount = data?.Count ?? 0
                });
        }

        return View(model);
    }
}