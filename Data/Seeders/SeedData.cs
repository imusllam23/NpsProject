using Microsoft.AspNetCore.Identity;
using NpsProject.Models;

namespace NpsProject.Data.Seeders
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // إنشاء الأدوار
            string[] roleNames = { "Admin", "Editor", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // إنشاء مستخدم أدمن افتراضي
            var adminEmail = "admin@company.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "مدير النظام",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    Console.WriteLine("✅ تم إنشاء مستخدم الأدمن الافتراضي:");
                    Console.WriteLine($"   البريد: {adminEmail}");
                    Console.WriteLine($"   كلمة المرور: Admin@123");
                }
            }

            // يمكنك إضافة بيانات تجريبية أخرى هنا
        }
    }
}
