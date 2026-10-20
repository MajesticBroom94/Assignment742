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
    private readonly IWebHostEnvironment _environment;

    public BookingController(ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        
    }

    
    [HttpGet]
    [Authorize(Roles = "Host")]
    public async Task<IActionResult> BookingRequest(BookingStatus status = BookingStatus.Pending)
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
            .OrderByDescending(b => b.CreatedOn)
            .Select(b => new BookingRequestViewModel
            {
                GuestId = b.GuestId,
                BookingId = b.Id,
                BookingStatus = b.Status,
                PropertyTitle = b.Property.Title,
                PropertyAddress = b.Property.Address,
                CreatedOn = b.CreatedOn,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                GuestDetails = b.Guest.FirstName + " " + b.Guest.LastName,
                GuestEmail = b.Guest.Email ?? "",
                
                PaymentStatus = b.Payment != null
                    ? b.Payment.Status
                    : PaymentStatus.PayLater,
                
                GuestDocumentId = b.GuestCheckIn == null
                    ? null : b.GuestCheckIn.GuestDocuments
                        .OrderByDescending(d => d.Id)
                        .Select(d => (int?)d.Id)
                        .FirstOrDefault(),

                GuestDocumentType = b.GuestCheckIn == null
                    ? null : b.GuestCheckIn.GuestDocuments
                        .OrderByDescending(d => d.Id)
                        .Select(d => d.DocumentType)
                        .FirstOrDefault(),

                GuestDocumentStatus = b.GuestCheckIn == null
                    ? null : b.GuestCheckIn.GuestDocuments
                        .OrderByDescending(d => d.Id)
                        .Select(d => (DocumentStatus?)d.Status)
                        .FirstOrDefault(),
                
            })
            .ToListAsync();

        ViewBag.SelectedStatus = status;

        return View(bookings);
    }
    
    [HttpPost]
    [Authorize(Roles = "Host")]
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
            .Where(b => b.Id == bookingId && b.Property.HostId == host.Id)
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

        if (booking.Property.Status != PropertyStatus.Active && status != BookingStatus.Rejected)
        {
            TempData["Error"] = $"The property for this booking is {booking.Property.Status}";

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
                
                var notifyApprove = new Notification
                {
                    UserId = booking.GuestId,
                    Title = $"Booking for the {booking.Property.Title} has been approved",
                    Type = NotificationType.BookingUpdate,
                    Status = NotificationStatus.Unread,
                };

                var logActivityApprove = new ActivityLog
                {
                    UserId = host.Id,
                    Action = "Booking Approved",
                    ActivityType = ActivityType.Update
                };
                
                _context.Notifications.Add(notifyApprove);
                _context.ActivityLogs.Add(logActivityApprove);

                booking.GuestCheckIn = guestCheckin;
                break;
            }
            
            // change payment to be not collected
            case BookingStatus.Rejected:

                if (booking.Payment != null)
                {
                    if (booking.Payment.Status == PaymentStatus.PayLater)
                    {
                        booking.Payment.Status = PaymentStatus.Canceled;
                    }
                    else if (booking.Payment.Status == PaymentStatus.Canceled)
                    {
                        booking.Payment.Status = PaymentStatus.Refunded;
                        
                        var notifyRefund = new Notification
                        {
                            UserId = booking.GuestId,
                            Title = $"You have been refunded R {booking.Payment.Amount} for your booking at " +
                                    $"{booking.Property.Title}",
                            Type = NotificationType.BookingUpdate,
                            Status = NotificationStatus.Unread,
                        };
                        
                        var logActivityRefund = new ActivityLog
                        {
                            UserId = host.Id,
                            Action =  "Booking Refund Processed",
                            ActivityType = ActivityType.Update
                        };

                        _context.Notifications.Add(notifyRefund);
                        _context.ActivityLogs.Add(logActivityRefund);
                    }
                }
                
                var notifyReject = new Notification
                {
                    UserId = booking.GuestId,
                    Title = $"Booking for the {booking.Property.Title} was rejected",
                    Type = NotificationType.BookingUpdate,
                    Status = NotificationStatus.Unread,
                };

                var logActivityReject = new ActivityLog
                {
                    UserId = host.Id,
                    Action = "Booking Rejected",
                    ActivityType = ActivityType.Update
                };
                
                _context.Notifications.Add(notifyReject);
                _context.ActivityLogs.Add(logActivityReject);
                
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
    [Authorize(Roles = "Guest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkin(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Challenge();

        var guestCheckIn = await _context.GuestCheckIns
            .Where(b => b.Booking.GuestId == user.Id && b.BookingId == bookingId)
            .Include(b => b.Booking)
            .ThenInclude(b => b.Property)
            .FirstOrDefaultAsync();

        if (guestCheckIn == null)
        {
            return NotFound();
        }
        
        if (guestCheckIn.Status != CheckInStatus.Pending)
            return BadRequest("Invalid status on the guest checkin workflow");
        
        var hasValidDocument = await _context.GuestDocuments
            .AnyAsync(d => d.GuestCheckIn.BookingId == bookingId &&
                                       d.GuestCheckIn.Booking.GuestId == user.Id &&
                                       d.Status == DocumentStatus.Valid);

        if (!hasValidDocument)
            return BadRequest("No valid guest documentation was found");

        guestCheckIn.Status = CheckInStatus.CheckedIn;
        
        var notify = new Notification
        {
            UserId = guestCheckIn.Booking.Property.HostId,
            Title = $"Guest has checked in for their booking at {guestCheckIn.Booking.Property.Title}" +
                    $" [Booking id: {guestCheckIn.Booking.Id}]",
            Type = NotificationType.CheckIn,
            Status = NotificationStatus.Unread,
        };

        var logActivity = new ActivityLog
        {
            UserId = user.Id,
            Action = "Guest Check In",
            ActivityType = ActivityType.Update
        };
        
        _context.Notifications.Add(notify);
        _context.ActivityLogs.Add(logActivity);
        
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Success: You have been checked in";
        return RedirectToAction(nameof(Index));

    }
    
    [HttpPost]
    [Authorize(Roles = "Guest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Challenge();

        var guestCheckIn = await _context.GuestCheckIns
            .Where(b => b.Booking.GuestId == user.Id && b.BookingId == bookingId)
            .Include(b => b.Booking.Property)
            .FirstOrDefaultAsync();

        if (guestCheckIn == null)
        {
            return NotFound();
        }
        
        if (guestCheckIn.Status != CheckInStatus.CheckedIn)
            return BadRequest();

        guestCheckIn.Status = CheckInStatus.CheckedOut;
        
        var notify = new Notification
        {
            UserId = guestCheckIn.Booking.Property.HostId,
            Title = $"Guest has checked out for their booking at {guestCheckIn.Booking.Property.Title}" +
                    $" [Booking id: {guestCheckIn.Booking.Id}]",
            Type = NotificationType.CheckOut,
            Status = NotificationStatus.Unread,
        };

        var logActivity = new ActivityLog
        {
            UserId = user.Id,
            Action = "Guest Check Out",
            ActivityType = ActivityType.Update
        };
        
        _context.Notifications.Add(notify);
        _context.ActivityLogs.Add(logActivity);
        
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Success: You have been checked out.";
        return RedirectToAction(nameof(Index));
    }
    

    [HttpPost]
    [Authorize(Roles = "Guest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Challenge();
        
        var booking = await  _context.Bookings
            .Where(b => b.Id == bookingId && b.GuestId == user.Id)
            .Include(b => b.Payment)
            .Include(b => b.Property)
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
        
        var notify = new Notification
        {
            UserId = booking.Property.HostId,
            Title = "Payment received for booking id: " + booking.Id,
            Type = NotificationType.UserPromotion,
            Status = NotificationStatus.Unread,
        };

        var logActivity = new ActivityLog
        {
            UserId = user.Id,
            Action = "Payment received",
            ActivityType = ActivityType.Update
        };
            
        _context.Notifications.Add(notify);
        _context.ActivityLogs.Add(logActivity);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Payment completed successfully.";
        
        return RedirectToAction(nameof(Index));
    }
    
    
    [HttpPost]
    [Authorize(Roles = "Guest")]
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

        if (model.PaymentOption != PaymentStatus.PayLater && model.PaymentOption != PaymentStatus.Completed)
        {
            return BadRequest();
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
        
        
        // BOOKING
        var booking = new Booking
        {
            GuestId = guest.Id,
            PropertyId = model.PropertyId,
            CheckInDate = model.CheckInDate,
            CheckOutDate = model.CheckOutDate,
            TotalPrice = totalPrice,
            Status = BookingStatus.Pending
        };
        
        // create the notification for property owner
        string message = $"New reservation received for: {property.Title}\n" +
                         $"Please react on this request as soon as possible";
        
        var notificationBookingRequest = new Notification
        {
            UserId = property.HostId,
            Title = message,
            Status = NotificationStatus.Unread,
            Type = NotificationType.BookingRequest
        };
        
        // LOGGING
        var logActivityBooking = new ActivityLog
        {
            UserId = guest.Id,
            Action = "New Booking",
            ActivityType = ActivityType.Create
        };
        
        _context.Bookings.Add(booking);
        _context.Notifications.Add(notificationBookingRequest);
        _context.ActivityLogs.Add(logActivityBooking);
        

        var affectedRows = await _context.SaveChangesAsync();

        if (affectedRows < 1)
        {
            TempData["BookingError"] = "Submission Was Not Successful. Please Try Again";
            return RedirectToAction("Details", "Property", new { id = model.PropertyId });
        }
        
        // PAYMENT
        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            Status = model.PaymentOption
        };
        
        var notificationBookingPayment = new Notification
        {
            UserId = property.HostId,
            Title = $"Payment received from {guest.FirstName} ({guest.Email}) for booking Id: {booking.Id}",
            Status = NotificationStatus.Unread,
            Type = NotificationType.BookingRequest
        };
        
        var logActivityPayment = new ActivityLog
        {
            UserId = guest.Id,
            Action = $"New Payment Created: Status - {payment.Status} ",
            ActivityType = ActivityType.Create
        };
        
        _context.Payments.Add(payment);
        _context.Notifications.Add(notificationBookingPayment);
        _context.ActivityLogs.Add(logActivityPayment);
        
        await _context.SaveChangesAsync();
        
        TempData["BookingConfirmation"] = "Your Reservation Has Been Submitted";
        return RedirectToAction("Details", "Property", new { id = model.PropertyId });
    }
 

    [Authorize(Roles = "Admin, SuperAdmin, Guest")]
    [HttpGet]
    public async Task<IActionResult> Index(int? filterId = null)
    {
        
        var currentUser = await _userManager.GetUserAsync(User);
        
        if  (currentUser == null)
            return Unauthorized(); 
        
        var isAdmin = User.IsInRole("Admin") ||  User.IsInRole("SuperAdmin");

        
        // query start
        var query = _context.Bookings.AsQueryable();

        if (!isAdmin)
            query = query.Where(b => b.GuestId == currentUser.Id);

        if (filterId != null)
        {
            query = query.Where(b => b.Id == filterId.Value);
        }
      
        var booking = await query
            .OrderByDescending(b=>b.CreatedOn)
            .Select(b => new BookingViewModel
        {
            BookingId = b.Id,
            HostId = b.Property.HostId,
            BookingDate = b.CreatedOn,
            guestDetails = b.Guest.FirstName + " " + b.Guest.LastName,
            guestEmail =  b.Guest.Email ?? string.Empty,
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
            
            LatestDocumentStatus = b.GuestCheckIn == null ? null : _context.GuestDocuments
                    .Where(d => d.GuestCheckInId == b.GuestCheckIn.Id)
                    .OrderByDescending(d => d.Id)
                    .Select(d => (DocumentStatus?)d.Status)
                    .FirstOrDefault(),
            
            HasGuestDocument = b.GuestCheckIn != null && _context.GuestDocuments.Any(doc =>
                                   doc.GuestCheckInId == b.GuestCheckIn.Id),

            HasValidGuestDocument = b.GuestCheckIn != null && _context.GuestDocuments.Any(doc =>
                                        doc.GuestCheckInId == b.GuestCheckIn.Id &&
                                        doc.Status == DocumentStatus.Valid),
        })
        .ToListAsync();
        
        return View(booking);
    }
    
    [HttpGet]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> BookingCheckinSteps(int bookingId)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var bookingExists = await _context.Bookings
            .AnyAsync(b =>
                b.Id == bookingId &&
                b.GuestId == user.Id &&
                b.Status == BookingStatus.Approved);

        if (!bookingExists)
            return NotFound();

        var process = await _context.GuestCheckIns
            .Where(c => c.BookingId == bookingId &&
                                    c.CheckInProcess != null)
            .Select(c => new
            {
                c.CheckInProcess!.Title,
                c.CheckInProcess.StepsJson
            })
            .FirstOrDefaultAsync();

        if (process == null)
        {
            return Json(new
            {
                title = "Check-in Instructions",
                steps = new List<string>
                {
                    "No check-in instructions are available for this property."
                }
            });
        }

        var steps = JsonSerializer.Deserialize<List<string>>(
            process.StepsJson) ?? new List<string>();

        return Json(new
        {
            title = process.Title,
            steps
        });
    }
    
    
    [HttpGet]
    [Authorize(Roles = "Host")]
    public async Task<IActionResult> ViewGuestDocument(int documentId)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var document = await _context.GuestDocuments
            .Where(d => d.Id == documentId &&
                        d.GuestCheckIn.Booking.Property.HostId == user.Id &&
                        d.GuestCheckIn.Booking.Status == BookingStatus.Approved)
            .Select(d => new
            {
                d.FileName
            })
            .FirstOrDefaultAsync();

        if (document == null)
            return NotFound();

        var directory = Path.Combine(
            _environment.ContentRootPath,
            "PrivateGuestDocuments");

        // Only allow the stored filename, not a filesystem path.
        var filePath = Path.Combine(
            directory,
            Path.GetFileName(document.FileName));

        if (!System.IO.File.Exists(filePath))
            return NotFound("Document file not found.");

        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();

        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };

        if (contentType == null)
            return BadRequest("Unsupported document format.");

        return PhysicalFile(filePath, contentType);
    }
    
    [HttpPost]
    [Authorize(Roles = "Host")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDocumentStatus(int documentId, DocumentStatus status)
    {
        if (status != DocumentStatus.Valid && status != DocumentStatus.Invalid)
            return BadRequest();

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();
        
        var guestDocument  = await _context.GuestDocuments
            .Where(d => d.Id == documentId && d.GuestCheckIn.Booking.Property.HostId == user.Id)
            .Include(d => d.GuestCheckIn)
            .ThenInclude(g => g.Booking)
            .FirstOrDefaultAsync();
        
        if (guestDocument == null)
            return NotFound();

        if (guestDocument.Status != DocumentStatus.Pending)
            return BadRequest("This document has already been reviewed.");

        
        // change the status
        guestDocument.Status = status;

        // booking info
        var bookingData = guestDocument.GuestCheckIn.Booking;
        
        var notification = new Notification
        {
            UserId = bookingData.GuestId,
            Title = $"Your uploaded document for booking {bookingData.Id} is " +
                    $"{(guestDocument.Status == DocumentStatus.Valid ? "valid" : "not valid")}",
            Status = NotificationStatus.Unread,
            Type = NotificationType.GuestDocument
        };

        var logActivity = new ActivityLog
        {
            UserId = user.Id,
            Action = $"{guestDocument.Status} guest booking document",
            ActivityType = ActivityType.Update
        };

        _context.Notifications.Add(notification);
        _context.ActivityLogs.Add(logActivity);
        
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document status updated.";

        return RedirectToAction("BookingRequest", new
        {
            status = BookingStatus.Approved
        });
    }
    
    
    [HttpPost]
    [Authorize(Roles = "Guest")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadGuestDocument(
        int bookingId,
        string documentType,
        IFormFile? document)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var guestCheckIn = await _context.GuestCheckIns
            .FirstOrDefaultAsync(g =>
                g.BookingId == bookingId &&
                g.Booking.GuestId == user.Id &&
                g.Booking.Status == BookingStatus.Approved);

        if (guestCheckIn == null)
            return Forbid();

        var redirect = RedirectToAction("Index", new { filterId = bookingId });

        // Prevent uploading after check-out
        if (guestCheckIn.Status == CheckInStatus.CheckedOut)
        {
            TempData["Error"] = "Documents cannot be uploaded after check-out.";
            return redirect;
        }

        // Only allow another upload after rejection
        var latestStatus = await _context.GuestDocuments
            .Where(d => d.GuestCheckInId == guestCheckIn.Id)
            .OrderByDescending(d => d.Id)
            .Select(d => (DocumentStatus?)d.Status)
            .FirstOrDefaultAsync();

        if (latestStatus is DocumentStatus.Valid or DocumentStatus.Pending)
        {
            TempData["Error"] = "A document is already approved or awaiting verification.";
            return redirect;
        }

        if (string.IsNullOrWhiteSpace(documentType) ||
            documentType.Length > 100)
        {
            TempData["Error"] = "Please provide a valid document type.";
            return redirect;
        }

        if (document == null || document.Length == 0)
        {
            TempData["Error"] = "Please select a document.";
            return redirect;
        }

        const long maxFileSize = 5 * 1024 * 1024;

        if (document.Length > maxFileSize)
        {
            TempData["Error"] = "The document cannot exceed 5 MB.";
            return redirect;
        }

        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

        if (!allowedExtensions.Contains(extension))
        {
            TempData["Error"] = "Only PDF, JPG and PNG files are supported.";
            return redirect;
        }

        // Check basic file signatures, not only the extension
        var header = new byte[8];

        using (var stream = document.OpenReadStream())
        {
            var bytesRead = await stream.ReadAsync(header.AsMemory());

            if (bytesRead < 4)
            {
                TempData["Error"] = "The uploaded file is invalid.";
                return redirect;
            }
        }

        bool validFile = extension switch
        {
            ".pdf" => header.AsSpan(0, 4)
                .SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }),

            ".jpg" or ".jpeg" => header.AsSpan(0, 3)
                .SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),

            ".png" => header.AsSpan(0, 8)
                .SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47,
                                            0x0D, 0x0A, 0x1A, 0x0A }),

            _ => false
        };

        if (!validFile)
        {
            TempData["Error"] = "The file contents do not match its extension.";
            return redirect;
        }

        // Store privately, outside wwwroot
        var uploadDirectory = Path.Combine(
            _environment.ContentRootPath,
            "PrivateGuestDocuments");

        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        try
        {
            await using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await document.CopyToAsync(stream);
            }

            var guestDocument = new GuestDocument
            {
                GuestCheckInId = guestCheckIn.Id,
                DocumentType = documentType.Trim(),
                FileName = fileName,
                Status = DocumentStatus.Pending
            };
            
            _context.GuestDocuments.Add(guestDocument);


            var bookingData = await _context.Bookings
                .Where(b => b.Id == bookingId)
                .Include(b => b.Property)
                .FirstAsync();
            
            var notification = new Notification
            {
                UserId = bookingData.Property.HostId,
                Title = $"A document was uploaded for booking {bookingData.Id}\n" +
                        $"Please review these documents as soon as possible",
                Status = NotificationStatus.Unread,
                Type = NotificationType.GuestDocument
            };

            var logActivity = new ActivityLog
            {
                UserId = user.Id,
                Action = $"Document uploaded for booking {bookingData.Id}",
                ActivityType = ActivityType.Create
            };

            _context.Notifications.Add(notification);
            _context.ActivityLogs.Add(logActivity);
            
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            throw;
        }

        TempData["Success"] = "Document uploaded successfully. Awaiting host verification.";

        return redirect;
    }

}