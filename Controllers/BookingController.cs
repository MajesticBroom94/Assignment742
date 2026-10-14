using System.Text.Json;
using Assignment742.Data;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Assignment742.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<BookingController> logger)
    {
        _context = context;
        _userManager = userManager;
    }

    
    [HttpGet]

    public async Task<IActionResult> BookingRequest(
        BookingStatus status = BookingStatus.Pending)
    {
        var host = await _userManager.GetUserAsync(User);

        if (host == null)
            return Challenge();

        if (!Enum.IsDefined(status))
            return BadRequest("Invalid booking status.");

        var bookings = await _context.Bookings
            .Where(b =>
                b.Property.HostId == host.Id &&
                b.Status == status)
            .OrderBy(b => b.CheckInDate)
            .Select(b => new BookingRequestViewModel
            {
                GuestId = b.GuestId,
                BookingId = b.Id,
                PropertyTitle = b.Property.Title,
                PropertyAddress = b.Property.Address,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,

                GuestDetails =
                    b.Guest.FirstName + " " + b.Guest.LastName,

                GuestEmail = b.Guest.Email ?? "",

                PaymentStatus = b.Payment != null
                    ? b.Payment.Status
                    : PaymentStatus.PayLater
            })
            .ToListAsync();

        ViewBag.SelectedStatus = status;

        return View(bookings);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequestStatus(int bookingId, BookingStatus status)
    {
        var host = await _userManager.GetUserAsync(User);

        if (host == null)
            return Challenge();

        if (status != BookingStatus.Approved && status != BookingStatus.Rejected)
        {
            return BadRequest();
        }

        var booking = await _context.Bookings
            .Where(b =>
                b.Id == bookingId &&
                b.Property.HostId == host.Id)
            .Include(b => b.Payment)
            .Include(b => b.Property)
            .ThenInclude(hostProperty => hostProperty.CheckInProcess)
            .FirstOrDefaultAsync();

        if (booking == null)
            return NotFound();

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["Error"] = "This booking request has already been processed.";

            return RedirectToAction(nameof(BookingRequest));
        }

        switch (status)
        {
            // create GuestCheckin
            case BookingStatus.Approved:
            {
                var guestCheckin = new GuestCheckIn
                {
                    BookingId = booking.Id,
                    Status = CheckInStatus.Pending,
                    CheckInProcessId = booking.Property.CheckInProcess?.Id
                };

                booking.GuestCheckIn = guestCheckin;
                break;
            }
            
            // change payment to be nopt collected
            case BookingStatus.Rejected when booking.Payment != null:
                booking.Payment.Status = PaymentStatus.Refunded;
                break;
        }

        booking.Status = status;

        await _context.SaveChangesAsync();

        TempData["Success"] = status == BookingStatus.Approved
            ? "Booking request approved successfully."
            : "Booking request was rejected.";

        return RedirectToAction(nameof(BookingRequest));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkin(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Challenge();

        var guestCheckIn = await _context.GuestCheckIns
            .Where(b => b.Booking.GuestId == user.Id && b.BookingId == bookingId)
            .FirstOrDefaultAsync();

        if (guestCheckIn == null)
        {
            return NotFound();
        }
        
        if (guestCheckIn.Status != CheckInStatus.Pending)
            return BadRequest();

        guestCheckIn.Status = CheckInStatus.CheckedIn;
        
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Success: You have been checked in";
        return RedirectToAction(nameof(Index));

    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Challenge();

        var guestCheckIn = await _context.GuestCheckIns
            .Where(b => b.Booking.GuestId == user.Id && b.BookingId == bookingId)
            .FirstOrDefaultAsync();

        if (guestCheckIn == null)
        {
            return NotFound();
        }
        
        if (guestCheckIn.Status != CheckInStatus.CheckedIn)
            return BadRequest();

        guestCheckIn.Status = CheckInStatus.CheckedOut;
        
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Success: You have been checked out.";
        return RedirectToAction(nameof(Index));
    }
    

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Challenge();
        
        var booking = await  _context.Bookings
            .Where(b => b.Id == bookingId && b.GuestId == user.Id)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync();
        
        if (booking == null)
            return NotFound();
        
        if (booking.Status == BookingStatus.Rejected)
            return BadRequest("This booking was cancelled by host");

        if (booking.Payment == null)
        {
            booking.Payment = new Payment
            {
                BookingId = booking.Id,
                Amount = booking.TotalPrice,
                Status = PaymentStatus.Completed
            };
        }

        else if (booking.Payment.Status == PaymentStatus.PayLater)
        {
            booking.Payment.Status = PaymentStatus.Completed;
        }
        else
        {
            return BadRequest();
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Payment completed successfully.";
        
        return RedirectToAction(nameof(Index));
    }
    
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        if (User.Identity is { IsAuthenticated: false })
        {
            
        }
        
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
                b.Status != BookingStatus.Rejected);

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

        if (affectedRows < 1)
        {
            TempData["BookingError"] = "Submission Was Not Successful. Please Try Again";
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }
        
        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            Status = model.PaymentOption
        };

        _context.Payments.Add(payment);
            
        await _context.SaveChangesAsync();

        // create the notification for property owner
        string message = $"New reservation received for: {property.Title}\n" +
                         $"You can approve this request under 'Bookings Request'";
        
        var notification = new Notification
        {
            UserId = property.HostId,
            Title = message,
            Status = NotificationStatus.Unread,
            Type = NotificationType.BookingRequest
        };
        
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        
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
                b.Status != BookingStatus.Rejected);

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

    [Authorize(Roles = "Admin, SuperAdmin, Guest")]
    [HttpGet]
    public async Task<IActionResult> Index(int? filterId = null)
    {
        
        var currentUser = await _userManager.GetUserAsync(User);
        
        if  (currentUser == null)
            return Unauthorized(); 
        
        var isAdmin = User.IsInRole("Admin") ||  User.IsInRole("SuperAdmin");

        var query = _context.Bookings.AsQueryable();

        if (!isAdmin)
            query = query.Where(b => b.GuestId == currentUser.Id);

        if (filterId != null)
        {
            query = query.Where(b => b.Id == filterId.Value);
        }
      
        var booking = await query.Select(b => new BookingViewModel
        {
            BookingId = b.Id,
            HostId = b.Property.HostId,
            PropertyTitle =  b.Property.Title,
            PropertyAddress =  b.Property.Address,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            HostDetails = b.Property.Host.FirstName + " " + b.Property.Host.LastName,
            HostEmail =  b.Property.Host.Email!,
            BookingStatus = b.Status,
            PaymentStatus = b.Payment == null ? PaymentStatus.PayLater : b.Payment.Status,
            CheckInProcessId = b.GuestCheckIn == null ? null : b.GuestCheckIn.CheckInProcessId,
            CheckInStatus = b.GuestCheckIn == null ? null : b.GuestCheckIn.Status,
        })
        .ToListAsync();
        
        return View(booking);
    }
    
    [HttpGet]
    public async Task<IActionResult> BookingCheckinSteps(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var bookingExists = await _context.Bookings
            .AnyAsync(b => b.Id == bookingId &&
                                  b.GuestId == user.Id &&
                                  b.Status == BookingStatus.Approved);

        if (!bookingExists)
            return NotFound();

        var process = await _context.GuestCheckIns
            .Where(c => c.BookingId == bookingId)
            .Select(c => new
            {
                c.CheckInProcess!.Title,
                c.CheckInProcess.StepsJson
            })
            .FirstOrDefaultAsync();

        if (process == null)
            return NotFound("No check-in instructions available.");

        var steps = JsonSerializer.Deserialize<List<string>>(process.StepsJson) ?? new List<string>();

        return Json(new
        {
            title = process.Title,
            steps
        });
    }
}