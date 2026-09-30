using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Assignment742.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BookingController> _logger;

    public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<BookingController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["BookingError"] = "Submission Was Not Successful. Please Try Again";
            
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }
        
        var guest = await _userManager.GetUserAsync(User);

        if (guest == null)
        {
            return Challenge();
        }
        
        // check date is not backdated
        if (model.CheckInDate.Date < DateTime.Today)
        {
            
            TempData["BookingError"] = "Check-in date cannot be in the past";
            
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }
        
        // check if duration is valid
        var stayDays = (model.CheckOutDate - model.CheckInDate).Days;

        if (stayDays <= 0)
        {
            TempData["BookingError"] = "Check-out date must be after check-in date";
            
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }

        // check if pay later status is valid
        var daysUntilCheckIn = (model.CheckInDate.Date - DateTime.Today).Days;

        if (daysUntilCheckIn < 7 && model.PaymentOption == PaymentStatus.PayLater)
        {
            TempData["BookingError"] = "Pay Later is only available when check-in is at least 7 days away";
            
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }
        
        var property = await _context.HostProperties
            .FirstOrDefaultAsync(p => p.Id == model.PropertyId);

        if (property == null)
        {
            TempData["BookingError"] = "Property Could Not Be Found!";
            
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }
        
        var hasConflict = await _context.Bookings
            .AnyAsync(b =>
                b.PropertyId == model.PropertyId &&
                b.CheckInDate < model.CheckOutDate &&
                b.CheckOutDate > model.CheckInDate &&
                b.Status != BookingStatus.Cancelled);

        if (hasConflict)
        {
            TempData["BookingError"] = "Unfortunately, the property is no longer available for the selected dates";
            
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }
        
        var accommodationPrice = stayDays * property.PricePerNight;
        var serviceFee = accommodationPrice * 0.10m;
        var totalPrice = accommodationPrice + serviceFee;
        

        var booking = new Booking
        {
            GuestId = guest.Id,
            PropertyId = model.PropertyId,
            CheckInDate = model.CheckInDate,
            CheckOutDate = model.CheckOutDate,
            TotalPrice = totalPrice,
            Status = BookingStatus.Pending
        };

        _context.Bookings.Add(booking);

        var affectedRows = await _context.SaveChangesAsync();

        if (affectedRows > 0)
        {
            var payment = new Payment
            {
                BookingId = booking.Id,
                Amount = booking.TotalPrice,
                Status = model.PaymentOption
            };

            _context.Payments.Add(payment);
            
            await _context.SaveChangesAsync();
        }
        
        TempData["BookingConfirmation"] = "Your Reservation Has Been Submitted";
        return RedirectToAction("Details", "Property", new { id = model.PropertyId });
    }
    
    [HttpGet]
    public async Task<IActionResult> CheckAvailability(
        int propertyId,
        DateTime checkInDate,
        DateTime checkOutDate)
    {
        // Basic date validation
        if (checkOutDate <= checkInDate)
        {
            return Json(new
            {
                available = false,
                message = "Check-out date must be after check-in date."
            });
        }

        if (checkInDate < DateTime.Today)
        {
            return Json(new
            {
                available = false,
                message = "Check-in date cannot be in the past."
            }); 
        }
        
        var propertyExists = await _context.HostProperties
            .AnyAsync(p => p.Id == propertyId);

        if (!propertyExists)
        {
            return Json(new
            {
                available = false,
                message = "Property could not be found."
            });
        }

        // Check for overlapping bookings
        var hasConflict = await _context.Bookings
            .AnyAsync(b =>
                b.PropertyId == propertyId &&
                b.CheckInDate < checkOutDate &&
                b.CheckOutDate > checkInDate &&
                b.Status != BookingStatus.Cancelled);

        if (hasConflict)
        {
            return Json(new
            {
                available = false,
                message = "The property is not available for the selected date range"
            });
        }

        return Json(new
        {
            available = true,
            message = "The property is available to reserve!"
        });
    }

}