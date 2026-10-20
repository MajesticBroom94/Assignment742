using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Assignment742.Data;

public static class PropertySeeder
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // check if seeding is needed
        var hasProperties = await context.HostProperties.AnyAsync();

        if (hasProperties)
            return;
        
        // find user to link properties to
        const string hostEmail = "host@staybnb.com";
        
        var host = await userManager.FindByEmailAsync(hostEmail);

        if (host == null)
            throw new InvalidOperationException("The property owner has not been seeded");
        
        var availableAmenities =  await context.Amenities.ToListAsync();
        
        // properties data
        var properties = new[]
        {
            new
            {
                Title = "Modern City Apartment",
                Description = "A stylish modern apartment with a fully equipped kitchen, comfortable furnishings, and easy access to restaurants and local attractions.",
                Price = 1200m,
                HostId = host.Id,
                Address = "123 Main Road, Sea Point, Cape Town",
                Beds = 2, Bedrooms = 1, Bathrooms = 1, MaxGuests = 2, Type = 1,
                AmenityIds = new[] { 7, 8, 11 },
                ImageUrl = "/uploads/properties/property1.jpeg"
            },
            new
            {
                Title = "Spacious Family House",
                Description = "A welcoming family home with a private garden, spacious living areas, and a fully equipped kitchen.",
                Price = 2500m,
                HostId = host.Id,
                Address = "45 Beach Road, Umhlanga, Durban",
                Beds = 4, Bedrooms = 3, Bathrooms = 2, MaxGuests = 6, Type = 0,
                AmenityIds = new[] { 5, 7, 16 },
                ImageUrl = "/uploads/properties/property2.jpeg"
            },
            new
            {
                Title = "Cozy Mountain Cottage",
                Description = "A peaceful cottage surrounded by nature, perfect for a relaxing weekend getaway with beautiful mountain views.",
                Price = 950m,
                HostId = host.Id,
                Address = "18 Mountain View Road, Stellenbosch",
                Beds = 2, Bedrooms = 1, Bathrooms = 1, MaxGuests = 2, Type = 2,
                AmenityIds = new[] { 1, 4, 6, 8, 10 },
                ImageUrl = "/uploads/properties/property3.jpeg"
            },
            new
            {
                Title = "Luxury Beachfront Villa",
                Description = "An elegant beachfront villa featuring spacious bedrooms, a private swimming pool, and stunning ocean views.",
                Price = 4500m,
                HostId = host.Id,
                Address = "77 Beachfront Drive, Camps Bay, Cape Town",
                Beds = 6, Bedrooms = 4, Bathrooms = 3, MaxGuests = 8, Type = 6,
                AmenityIds = new[] { 4, 9, 15 },
                ImageUrl = "/uploads/properties/property4.jpeg"
            },
            new
            {
                Title = "Rustic Country Farmhouse",
                Description = "A charming farmhouse offering a quiet countryside escape, outdoor spaces, and beautiful agricultural surroundings.",
                Price = 1800m,
                HostId = host.Id,
                Address = "12 Farm Road, Paarl",
                Beds = 4, Bedrooms = 3, Bathrooms = 2, MaxGuests = 6, Type = 5,
                AmenityIds = new[] { 6, 8, 13 },
                ImageUrl = "/uploads/properties/property5.jpeg"
            },
            new
            {
                Title = "Boutique Guest House",
                Description = "A comfortable guest house with thoughtfully decorated rooms, welcoming common areas, and convenient local amenities.",
                Price = 1100m,
                HostId = host.Id,
                Address = "9 Church Street, Stellenbosch",
                Beds = 3, Bedrooms = 2, Bathrooms = 1, MaxGuests = 4, Type = 3,
                AmenityIds = new[] { 3, 4, 7, 12},
                ImageUrl = "/uploads/properties/property6.jpeg"
            },
            new
            {
                Title = "Woodland Log Cabin",
                Description = "A secluded wooden cabin with a cozy interior, a fireplace, and a peaceful setting ideal for couples or small families.",
                Price = 850m,
                HostId = host.Id,
                Address = "6 Forest Lane, Hogsback",
                Beds = 2, Bedrooms = 1, Bathrooms = 1, MaxGuests = 2, Type = 8,
                AmenityIds = new[] { 2, 4, 8, 10 },
                ImageUrl = "/uploads/properties/property7.jpeg"
            },
            new
            {
                Title = "Elegant Garden Bungalow",
                Description = "A charming bungalow with a private garden, bright interiors, and a comfortable open-plan living area.",
                Price = 1400m,
                HostId = host.Id,
                Address = "32 Garden Avenue, Rosebank, Johannesburg",
                Beds = 3, Bedrooms = 2, Bathrooms = 2, MaxGuests = 4, Type = 7,
                AmenityIds = new[] { 5, 7, 11 },
                ImageUrl = "/uploads/properties/property8.jpeg"
            }
        };
        
        // add each property
        foreach (var propertyItem in properties)
        {
            var property = new HostProperty
            {
                Title = propertyItem.Title,
                Description = propertyItem.Description,
                PricePerNight = propertyItem.Price,
                HostId = propertyItem.HostId,
                Address = propertyItem.Address,
                Beds = propertyItem.Beds,
                Bedrooms = propertyItem.Bedrooms,
                Bathrooms = propertyItem.Bathrooms,
                MaxGuests = propertyItem.MaxGuests,
                // enums
                Status = PropertyStatus.Active,
                Type = (PropertyType)propertyItem.Type
            };

            context.HostProperties.Add(property);
            await context.SaveChangesAsync();
            
            // associate images with properties
            context.PropertyImages.Add(new PropertyImage
            {
                PropertyId = property.Id,
                ImageUrl = propertyItem.ImageUrl
            });

            // add amenities
            property.Amenities = availableAmenities
                .Where(available => propertyItem.AmenityIds.Contains(available.Id) )
                .ToList();
            
            context.HostProperties.Update(property);
        }
        
        await context.SaveChangesAsync();
    }
}