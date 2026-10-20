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
public class PropertyController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PropertyController> _logger;

    public PropertyController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        ILogger<PropertyController> logger)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin, SuperAdmin, Host")]
    public async Task<IActionResult> Index()
    {
        var query = _context.HostProperties.AsQueryable();

        if (User.IsInRole("Host"))
        {
            query = query.Where(p => p.HostId == _userManager.GetUserId(User));
        }
        
        var property = await query
           .Select(p => new HostPropertyViewModel
                               {
                                   Id = p.Id,
                                   HostId = p.HostId,
                                   Title = p.Title,
                                   Description = p.Description,
                                   PricePerNight = p.PricePerNight,
                                   Address = p.Address,
                                   Status = p.Status,
                                   Type = p.Type,
                                   Beds =  p.Beds,
                                   Bedrooms =  p.Bedrooms,
                                   Bathrooms = p.Bathrooms,
                                   MaxGuests = p.MaxGuests,
                                   Bookings = p.Bookings.Count,
                                   Reviews = p.Reviews.Count,
                                   ImageUrl = p.PropertyImages.Select(pi => pi.ImageUrl).FirstOrDefault() ?? ""
                               }).ToListAsync();
        
       return View(property);
    }

    
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();
        
        var currentUser = await _userManager.GetUserAsync(User);
        
        var p = await _context.HostProperties.
            Where(p => p.Id == id)
            .Select(p => new PropertyDetailsViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                PricePerNight = p.PricePerNight,
                Address = p.Address,
                Beds =  p.Beds,
                Bedrooms =  p.Bedrooms,
                Bathrooms = p.Bathrooms,
                MaxGuests = p.MaxGuests,
                Bookings = p.Bookings.Count,
                Reviews = p.Reviews.Count,
                PropertyType = p.Type,
                ImageUrls = p.PropertyImages.Select(pi => pi.ImageUrl).ToList(),
                Amenities = p.Amenities.ToList(),
                HostName = p.Host.FirstName + " " + p.Host.LastName,
                HostProfilePicture = p.Host.ProfilePictureUrl ?? "",
           
                ReviewContent = p.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .ThenByDescending(r => r.Id)
                    .Take(10)
                    .Select(r => new Review
                    {
                        Id = r.Id,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,

                        Reviewer = r.Reviewer
                    })
                    .ToList()
                
            }).FirstOrDefaultAsync();
       
        if (p == null)
        {
            return NotFound();
        }
        
        return View(p);
    }

    [HttpGet]
    public async Task<IActionResult> Search(SearchPropertyViewModel searchParameter)
    {
        if (string.IsNullOrWhiteSpace(searchParameter.Address) &&
            !searchParameter.Type.HasValue &&
            !searchParameter.GuestCount.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }
        
        var user = await _userManager.GetUserAsync(User);

        var query = _context.HostProperties
            .Where(p => p.Status == PropertyStatus.Active)
            .AsQueryable();

        // Filter by location if provided
        if (!string.IsNullOrWhiteSpace(searchParameter.Address))
        {
            query = query.Where(p => p.Address
                          .Contains(searchParameter.Address));
        }

        // Filter by guest count if provided
        if (searchParameter.GuestCount.HasValue)
        {
            query = query.Where(p => p.MaxGuests >= searchParameter.GuestCount);
        }

        // Filter by property type if provided
        if (searchParameter.Type.HasValue)
        {
            query = query.Where(p => p.Type == searchParameter.Type);
        }

        var model = await query
            .Select(p => new HomepageViewModel
            {
                Id = p.Id,
                Address = p.Address,
                Title = p.Title,
                PricePerNight = p.PricePerNight,
                ImageUrl = p.PropertyImages
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault() ?? "",
                ReviewCount = p.Reviews.Count,
                Rating = p.Reviews.Any()
                    ? p.Reviews.Average(r => r.Rating)
                    : 0.0,
                IsFavorite = user != null && p.WishlistItems.Any(w => w.UserId == user.Id)
            })
            .ToListAsync();

        ViewBag.SearchData = searchParameter;
        return View(model);
        
    }
    
    [HttpGet]
    [Authorize(Roles = "Guest, Host")]
    public async Task<IActionResult> Create()
    {
        // check to see if an existing host application exists for the guest user
        if (User.IsInRole("Guest"))
        {
            var guest = await _userManager.GetUserAsync(User);
            if (guest == null)
            {
                return Challenge();
            }
            var hasPendingApplication = await _context.HostApplications
                .AnyAsync(ha => ha.ApplicationUserId == guest.Id &&
                                ha.Status == ApplicationStatus.Pending);
            if (hasPendingApplication)
            {
                TempData["ApplicationError"] = "You have already applied to become a host";
                return RedirectToAction("Index", "Home");
            } 
        }
        
        ViewBag.AvailableAmenities = await _context.Amenities.ToListAsync();
        return View();
    }
    
    [Authorize(Roles = "Guest, Host")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePropertyViewModel model)
    {
        
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        foreach (var image in model.Images)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    nameof(model.Images),
                    "Only JPG, PNG and WEBP images are allowed."
                );

                break;
            }
        }
        
        ViewBag.AvailableAmenities = await _context.Amenities.ToListAsync();
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var propertyOwner = await _userManager.GetUserAsync(User);

        if (propertyOwner == null) 
        {
            return Challenge();
        }

        var property = new HostProperty
        {
            Title = model.Title,
            Description = model.Description,
            PricePerNight = model.PricePerNight,
            Address = model.Address,
            Type =  model.Type,
            Beds = model.Beds,
            Bathrooms = model.Bathrooms,
            Bedrooms = model.Bedrooms,
            MaxGuests = model.MaxGuests,
            HostId = propertyOwner.Id
        };
        
        // Add amenities
        var amenities = await _context.Amenities
            .Where(a => model.AmenityIds.Contains(a.Id))
            .ToListAsync();

        foreach (var amenity in amenities)
        {
            property.Amenities.Add(amenity);
        }

        // logging
        var logActivityPropertyCreate = new ActivityLog
        {
            UserId = propertyOwner.Id,
            Action =  "New Property Created",
            ActivityType = ActivityType.Create
        };
        
        _context.ActivityLogs.Add(logActivityPropertyCreate);    
        
        // Save property first so it gets an Id
        _context.HostProperties.Add(property);
        await _context.SaveChangesAsync();

        // Save uploaded images
        if (model.Images.Any())
        {
            var uploadDirectory = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "properties");

            Directory.CreateDirectory(uploadDirectory);

            foreach (var image in model.Images)
            {
                if (image.Length <= 0)
                    continue;

                var extension = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";

                var filePath = Path.Combine(uploadDirectory, fileName);

                await using var stream = new FileStream(
                    filePath,
                    FileMode.Create);

                await image.CopyToAsync(stream);

                var propertyImage = new PropertyImage
                {
                    PropertyId = property.Id,
                    ImageUrl = $"/uploads/properties/{fileName}"
                };

                _context.PropertyImages.Add(propertyImage);
            }
        }
        
        // save the checkin process
        
        var checkInProcess = new CheckInProcess
        {
            PropertyId = property.Id,
            Title = model.CheckInProcessTitle,
            StepsJson = model.CheckInProcessJson
        };
        
        _context.CheckInProcesses.Add(checkInProcess);
        
        // create host application for guest role applicant
        if (User.IsInRole("Guest"))
        {
            var hostApplication = new HostApplication
            {
                ApplicationUserId = propertyOwner.Id,
                PropertyId = property.Id,
                Status = ApplicationStatus.Pending
            };

            property.Status = PropertyStatus.AwaitingApproval;
            _context.HostApplications.Add(hostApplication);

            var inboxApplicant = new Notification
            {
                UserId = propertyOwner.Id,
                Title = $"Host application submitted successfully\n" +
                        $"Our team will review your submission as soon as possible",
                Status = NotificationStatus.Unread,
                Type = NotificationType.HostApplication
            };
            
            _context.Notifications.Add(inboxApplicant);
            
            // notify all admins
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var allAdmins = admins.UnionBy(superAdmins, user => user.Id ).ToList();

            foreach (var admin in allAdmins)
            {
                var notify = new Notification
                {
                    UserId = admin.Id,
                    Title = $"New Host Application By {propertyOwner.FirstName} - {propertyOwner.Email}",
                    Status = NotificationStatus.Unread,
                    Type = NotificationType.HostApplication
                };
                _context.Notifications.Add(notify);
            }
            
            // logging
            var logHostApplication = new ActivityLog()
            {
                UserId = propertyOwner.Id,
                Action = "New Host Application Created",
                ActivityType = ActivityType.Create,
            };
            _context.ActivityLogs.Add(logHostApplication);
            TempData["HostApplication"] = "Your host application and property was submitted";
        }
        else
        {
            property.Status = PropertyStatus.Active;
        }
        
        await _context.SaveChangesAsync();

        return User.IsInRole("Guest") ? 
            RedirectToAction("Index", "Home") : RedirectToAction(nameof(Index));
    }
    
    
    [HttpGet]
    public async Task<IActionResult> Update(int propertyId)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }
        
        var existingProperty = await _context.HostProperties
            .Where(p => p.Id == propertyId)
            .Where(p => p.HostId == currentUser.Id)
            .Select(p => new UpdatePropertyViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Address = p.Address,
                PricePerNight = p.PricePerNight,
                Beds = p.Beds,
                Bathrooms = p.Bathrooms,
                MaxGuests = p.MaxGuests,
                Bedrooms = p.Bedrooms,
                Type = p.Type,
                CheckInProcessTitle = p.CheckInProcess != null ? p.CheckInProcess.Title : "",
                CheckInProcessJson = p.CheckInProcess != null ? p.CheckInProcess.StepsJson : "",
                AmenityIds = p.Amenities.Select(a => a.Id).ToList(),
                ExistingImageUrls = p.PropertyImages.Select(image => image.ImageUrl).ToList()
            })
            .FirstOrDefaultAsync();

        if (existingProperty == null)
        {
            return NotFound();
        }
        
        ViewBag.AvailableAmenities = await _context.Amenities.ToListAsync();

        return View(existingProperty);
    }
    
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int propertyId, UpdatePropertyViewModel model)
    {
        if (propertyId != model.Id)
        {
            return BadRequest();
        }

        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }

        // Retrieve property and its relationships.
        var property = await _context.HostProperties
            .Include(p => p.Amenities)
            .Include(p => p.PropertyImages)
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == currentUser.Id);

        if (property == null)
        {
            return NotFound();
        }

        // Validate check-in JSON.
        try
        {
            var steps = JsonSerializer.Deserialize<List<string>>(
                model.CheckInProcessJson);

            if (steps == null)
            {
                ModelState.AddModelError(
                    nameof(model.CheckInProcessJson),
                    "Invalid check-in instructions.");
            }
        }
        catch (JsonException)
        {
            ModelState.AddModelError(
                nameof(model.CheckInProcessJson),
                "Invalid check-in instructions.");
        }

        // Validate uploaded files, if any.
        var allowedExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        foreach (var image in model.Images ?? new List<IFormFile>())
        {
            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    nameof(model.Images),
                    "Only JPG, PNG and WEBP images are allowed.");

                break;
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.AvailableAmenities = await _context.Amenities.ToListAsync();

            model.ExistingImageUrls = property.PropertyImages
                .Select(i => i.ImageUrl)
                .ToList();

            return View(model);
        }

        // UPDATE BASIC PROPERTY INFORMATION
        property.Title = model.Title;
        property.Description = model.Description;
        property.PricePerNight = model.PricePerNight;
        property.Address = model.Address;
        property.Type = model.Type;
        property.Beds = model.Beds;
        property.Bedrooms = model.Bedrooms;
        property.Bathrooms = model.Bathrooms;
        property.MaxGuests = model.MaxGuests;

        // UPDATE AMENITIES
        var selectedAmenities = await _context.Amenities
            .Where(a => model.AmenityIds.Contains(a.Id))
            .ToListAsync();

        property.Amenities.Clear();

        foreach (var amenity in selectedAmenities)
        {
            property.Amenities.Add(amenity);
        }

        // UPDATE CHECK-IN PROCESS
        if (property.CheckInProcess != null)
        {
            property.CheckInProcess.Title = model.CheckInProcessTitle;
            property.CheckInProcess.StepsJson = model.CheckInProcessJson;
        }
        else
        {
            // Handle properties without existing instructions.
            property.CheckInProcess = new CheckInProcess
            {
                PropertyId = property.Id,
                Title = model.CheckInProcessTitle,
                StepsJson = model.CheckInProcessJson
            };
        }

        // REPLACE IMAGES ONLY IF NEW ONES WERE UPLOADED
        var uploadedFiles = new List<string>();
        var oldImageUrls = new List<string>();

        if (model.Images != null && model.Images.Any())
        {
            var uploadDirectory = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "properties");

            Directory.CreateDirectory(uploadDirectory);

            try
            {
                // Upload new images before removing old ones.
                foreach (var image in model.Images)
                {
                    if (image.Length <= 0)
                        continue;

                    var extension = Path.GetExtension(image.FileName)
                        .ToLowerInvariant();

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadDirectory, fileName);

                    await using (var stream = new FileStream(
                        filePath, FileMode.CreateNew))
                    {
                        await image.CopyToAsync(stream);
                    }

                    uploadedFiles.Add(filePath);
                }
            }
            catch
            {
                // Remove any newly uploaded files if upload fails.
                foreach (var file in uploadedFiles)
                {
                    if (System.IO.File.Exists(file))
                    {
                        System.IO.File.Delete(file);
                    }
                }

                throw;
            }

            // Only replace images if at least one valid file was uploaded.
            if (uploadedFiles.Any())
            {
                oldImageUrls = property.PropertyImages
                    .Select(i => i.ImageUrl)
                    .ToList();

                // Remove existing database image records.
                _context.PropertyImages.RemoveRange(
                    property.PropertyImages.ToList());

                // Add new database image records.
                foreach (var filePath in uploadedFiles)
                {
                    var fileName = Path.GetFileName(filePath);

                    _context.PropertyImages.Add(new PropertyImage
                    {
                        PropertyId = property.Id,
                        ImageUrl = $"/uploads/properties/{fileName}"
                    });
                }
            }
        }

        // SAVE DATABASE CHANGES
        try
        {
            var logActivity = new ActivityLog
            {
                UserId = currentUser.Id,
                Action = $"Property {model.Id} updated",
                ActivityType = ActivityType.Update
            };
            _context.ActivityLogs.Add(logActivity);
            
            await _context.SaveChangesAsync();
            
        }
        catch
        {
            // Database save failed: clean up newly uploaded files.
            foreach (var file in uploadedFiles)
            {
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                }
            }

            throw;
        }

        // Database save succeeded: remove old physical image files.
        foreach (var imageUrl in oldImageUrls)
        {
            var fileName = Path.GetFileName(imageUrl);

            var oldFilePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "properties",
                fileName);

            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }
        }

        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin, SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PropertySuspensionManager(int propertyId, PropertyStatus newStatus)
    {
        
        var admin = await _userManager.GetUserAsync(HttpContext.User);
        if (admin == null)
            return NotFound();
        
        if (newStatus is not (PropertyStatus.Inactive or PropertyStatus.Suspended))
            return BadRequest();

        var property = await _context.HostProperties
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property == null)
            return NotFound();

        // Admin may suspend Active or Inactive properties.
        if (newStatus == PropertyStatus.Suspended &&
            property.Status is not (PropertyStatus.Active or PropertyStatus.Inactive))
        {
            return BadRequest("This property cannot be suspended.");
        }

        // Admin may only enable previously suspended properties.
        if (newStatus == PropertyStatus.Inactive && property.Status != PropertyStatus.Suspended)
        {
            return BadRequest("The Current Property status forbids this action");
        }

        property.Status = newStatus;
        
        
        var notify = new Notification
        {
            UserId = property.HostId,
            Title = $"Property {property.Title} (Id: {property.Id}) status changed to {newStatus}",
            Type = NotificationType.PropertyStatus,
            Status = NotificationStatus.Unread,
        };

        var logActivity = new ActivityLog
        {
            UserId = admin.Id,
            Action = $"Property status changed to {newStatus}",
            ActivityType = ActivityType.Update
        };
        
        _context.Notifications.Add(notify);
        _context.ActivityLogs.Add(logActivity);
        
        
        await _context.SaveChangesAsync();

        TempData["Success"] = newStatus == PropertyStatus.Suspended
            ? "Property suspended successfully."
            : "Property enabled successfully.";

        return RedirectToAction(nameof(Index));
    }
    
    /*
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectProperty(int propertyId)
    {
        var property = await _context.HostProperties
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property == null)
            return NotFound();

        if (property.Status == PropertyStatus.Suspended)
        {
            TempData["Error"] = "Property is already suspended.";
            return RedirectToAction(nameof(Index));
        }

        property.Status = PropertyStatus.Suspended;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Property suspended successfully.";

        return RedirectToAction(nameof(Index));
    }*/
    
    
    [HttpPost]
    [Authorize(Roles = "Host")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableDisableProperty(
        [FromForm] ChangePropertyStatusRequest? request)
    {
        if (request == null ||
            !ModelState.IsValid ||
            !request.PropertyId.HasValue ||
            !request.PropertyStatus.HasValue)
        {
            return BadRequest("Missing or invalid parameters.");
        }

        int propertyId = request.PropertyId.Value;
        PropertyStatus requestedStatus = request.PropertyStatus.Value;

        if (requestedStatus is not
            (PropertyStatus.Active or PropertyStatus.Inactive))
        {
            return BadRequest("Invalid property status.");
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var property = await _context.HostProperties
            .FirstOrDefaultAsync(p =>
                p.Id == propertyId &&
                p.HostId == user.Id);

        if (property == null)
            return NotFound();

        if (property.Status is not
            (PropertyStatus.Active or PropertyStatus.Inactive))
        {
            TempData["Error"] =
                "This property's status cannot be changed.";

            return RedirectToAction(nameof(Index));
        }

        if (property.Status != requestedStatus)
        {
            property.Status = requestedStatus;
            
            var logActivity = new ActivityLog
            {
                UserId = user.Id,
                Action = "Property status changed to " + requestedStatus,
                ActivityType = ActivityType.Update
            };
        
            _context.ActivityLogs.Add(logActivity);
            
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = requestedStatus == PropertyStatus.Active
            ? "Property enabled successfully."
            : "Property disabled successfully.";

        return RedirectToAction(nameof(Index));
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

 }
