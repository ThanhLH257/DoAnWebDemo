using Microsoft.AspNetCore.Identity;
using DoAnWebDemo.Models;

namespace DoAnWebDemo.Data // Nhớ đổi namespace theo đúng Project của bạn nếu cần
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Tự động tạo 2 chức danh: Admin và Khách Hàng (Borrower)
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("Borrower"))
                await roleManager.CreateAsync(new IdentityRole("Borrower"));

            // 2. Tự động tạo 1 tài khoản Admin mặc định
            var adminEmail = "admin@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Giám Đốc Hệ Thống",
                    EmailConfirmed = true // Bỏ qua bước xác nhận Email
                };

                // Đặt mật khẩu mặc định là: Admin@123
                var result = await userManager.CreateAsync(newAdmin, "Admin@123");
                if (result.Succeeded)
                {
                    // Gắn chức danh "Admin" cho tài khoản này
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}
