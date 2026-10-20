using Assignment742.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Assignment742.Data;

public static class RoleSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        string[] roleNames = { "Admin", "SuperAdmin", "Host", "Guest" };

        foreach (var role in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create a default Super Admin user
        const string adminEmail = "super.admin@staybnb.com";
        var adminPassword = configuration["SuperAdmin:Password"]
                            ?? throw new InvalidOperationException("SuperAdmin password is not configured.");
        
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            var newSuperAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail, 
                ProfilePictureUrl = "/Icons/profile.png",
                EmailConfirmed = true,
                FirstName = "StayBnb",
                LastName = "SuperAdmin",
            };
            var result = await userManager.CreateAsync(newSuperAdmin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newSuperAdmin, "SuperAdmin");
            }
        }
        
        // Create a Host user 
        const string hostEmail = "host@staybnb.com";
        
        var hostUser = await userManager.FindByEmailAsync(hostEmail);
        var hostPassword = configuration["Host:Password"]
                            ?? throw new InvalidOperationException("Host password is not configured.");
        
        if (hostUser == null)
        {
            var newHost = new ApplicationUser
            {
                UserName = hostEmail,
                Email = hostEmail, 
                ProfilePictureUrl = "/Icons/profile.png",
                EmailConfirmed = true,
                FirstName = "StayBnb",
                LastName = "Host",
            };
            var result = await userManager.CreateAsync(newHost, hostPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newHost, "Host");
            }
        }
    }
}