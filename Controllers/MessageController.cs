using Assignment742.Data;
using Assignment742.Domain.Models;
using Assignment742.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Controllers;

public class MessageController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Chat(string? contactId)
    {
        if (string.IsNullOrEmpty(contactId))
            return RedirectToAction(nameof(Index));
        
        var currentUser = await _userManager.GetUserAsync(User);
        var receiver = await _userManager.FindByIdAsync(contactId);

        if (currentUser == null || receiver == null)
            return BadRequest();
        
        var data = await _context.Messages
            .Where(m => m.SenderId == currentUser.Id ||
                                m.ReceiverId == currentUser.Id)
            .Where(m => m.SenderId == receiver.Id ||
                                m.ReceiverId == receiver.Id)
            .Select(m => new MessageViewModel
            {
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                ReceiverDetails = m.Receiver!.FirstName + " " + m.Receiver.LastName,
                SenderDetails = m.Sender!.FirstName + " " + m.Sender.LastName,
                Content = m.Content,
                Timestamp = m.Timestamp
            })
            .ToListAsync();
        
        ViewBag.ContactId = receiver.Id;
        ViewBag.ContactDetails = receiver.FirstName + " " + receiver.LastName;
        
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Challenge();
        }
        
        var contactIds = await _context.Messages
            .Where(m => m.SenderId == currentUser.Id ||
                                m.ReceiverId == currentUser.Id)
            .Select(m => m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToListAsync();

        var conversations = await _context.Users
            .Where(u => contactIds.Contains(u.Id))
            .Select(u => new MessageHistoryViewModel
            {
                ContactId = u.Id,

                ContactDetails = u.FirstName + " " + u.LastName,

                ContactProfilePicture = u.ProfilePictureUrl ?? "/Icons/profile.png",

                LastMessage = _context.Messages
                                  .Where(m =>
                                      (m.SenderId == currentUser.Id &&
                                       m.ReceiverId == u.Id) ||
                                      (m.SenderId == u.Id &&
                                       m.ReceiverId == currentUser.Id))
                                  .Max(m => (DateTime?)m.Timestamp)
                              ?? DateTime.MinValue
            })
            .OrderByDescending(c => c.LastMessage)
            .ToListAsync();
        
        return View(conversations);
        
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage(SendMessageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Could not send message";
            return RedirectToAction(nameof(Chat), new { contactId = model.ReceiverId });
        }
        
        var sender = await _userManager.GetUserAsync(User);
        var receiver = await _userManager.FindByIdAsync(model.ReceiverId);

        if (sender == null || receiver == null)
            return RedirectToAction(nameof(Index));
        
        var message = new Message
        {
            SenderId = sender.Id,
            ReceiverId = receiver.Id,
            Content = model.Content,
        };
        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Chat), new { contactId = model.ReceiverId });

    }
    
}