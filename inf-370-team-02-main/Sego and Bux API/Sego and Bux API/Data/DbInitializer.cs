//using Microsoft.EntityFrameworkCore;
//using Sego_and__Bux.Data;
//using Sego_and__Bux.Helpers;
//using Sego_and__Bux.Models;

//namespace Sego_and__Bux.Seeding
//{
//    public static class DbInitializer
//    {
//        public static void SeedData(WebApplication app)
//        {
//            using var scope = app.Services.CreateScope();
//            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//            context.Database.Migrate();

//            // ✅ Seed Employees
//            if (!context.Employees.Any())
//            {
//                context.Employees.AddRange(
//                    new Employee { Username = "admin", PasswordHash = PasswordHasher.HashPassword("Admin123!"), Role = "Admin" },
//                    new Employee { Username = "staff", PasswordHash = PasswordHasher.HashPassword("Employee123!"), Role = "Employee" }
//                );
//            }

//            // ✅ Seed Customers
//            if (!context.Customers.Any())
//            {
//                context.Customers.Add(new Customer
//                {
//                    Name = "John",
//                    Surname = "Doe",
//                    Email = "john@example.com",
//                    Phone = "0123456789",
//                    Address = "123 Main St",
//                    PasswordHash = PasswordHasher.HashPassword("Customer123!")
//                });
//            }

//            // ✅ Seed Categories and Products
//            if (!context.Categories.Any())
//            {
//                var fashion = new Category
//                {
//                    CategoryName = "Fashion",
//                    Description = "Category for fashion-related products"
//                };
//                context.Categories.Add(fashion);

//                var type = new ProductType
//                {
//                    ProductTypeName = "Shirts",
//                    Description = "All kinds of shirts",
//                    Category = fashion
//                };
//                context.ProductTypes.Add(type);

//                context.Products.Add(new Product
//                {
//                    Name = "Formal Shirt",
//                    Description = "White cotton shirt",
//                    Price = 399.99m,
//                    ProductType = type
//                });
//            }

//            context.SaveChanges();
//        }
//    }
//}
