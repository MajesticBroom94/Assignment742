using Assignment742.Data;
using Assignment742.Domain.Models;
using Assignment742.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Assignment742.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    
    public AccountController(UserManager<ApplicationUser> userManager,
                            SignInManager<ApplicationUser> signInManager,
                            ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }
    
    public IActionResult Index()
    {
        if (!_signInManager.IsSignedIn(User))
        {
            return RedirectToAction("Login");
        }
        
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        if (User.IsInRole("Host"))
        {
            return RedirectToAction("Dashboard", "Host");
        }
        
        return RedirectToAction("Index", "Home");
    }
    
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) 
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return RedirectToAction("Index");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager
                .PasswordSignInAsync(model.Email, 
                                        model.Password, 
                                        isPersistent: false, 
                                        lockoutOnFailure: false);
    
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Invalid login attempt");
        }
    
        return View(model);
    }
    
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return RedirectToAction("Index");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        if (model.Password != model.ConfirmPassword)
            ModelState.AddModelError(string.Empty, "Passwords do not match");
        
        if (ModelState.IsValid)
        {
            ApplicationUser newUser = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                ProfilePictureUrl = "/Icons/profile.png"
            };
            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Guest");
                await _signInManager.SignInAsync(newUser, false);
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Index");
            }
       
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
        }
        return View(model);
    }
    
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        
        return RedirectToAction("Index");
    }
}