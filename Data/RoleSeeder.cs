using Assignment742.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Assignment742.Data;

public static class RoleSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roleNames = { "Admin", "SuperAdmin", "Host", "Guest" };

        foreach (var role in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create a default Admin user
        var adminEmail = "super.admin@staybnb.com";
        
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            var newSuperAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail, 
                EmailConfirmed = true,
                FirstName = "StayBnb",
                LastName = "SuperAdmin",
            };
            var result = await userManager.CreateAsync(newSuperAdmin, "AdminPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newSuperAdmin, "SuperAdmin");
            }
        }
    }
}