using Microsoft.AspNetCore.Mvc;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DoAnWebDemo.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        // Tiêm IWebHostEnvironment để lấy đường dẫn thư mục wwwroot
        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        // 1. Hiển thị trang Hồ sơ
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        // 2. Xử lý khi khách hàng bấm nút Upload Ảnh
        [HttpPost]
        public async Task<IActionResult> UploadCCCD(IFormFile cccdImage)
        {
            var user = await _userManager.GetUserAsync(User);

            if (cccdImage != null && cccdImage.Length > 0)
            {
                // Tạo tên file duy nhất tránh trùng lặp
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(cccdImage.FileName);

                // Trỏ đường dẫn tới thư mục wwwroot/uploads
                string uploadPath = Path.Combine(_env.WebRootPath, "uploads");
                string filePath = Path.Combine(uploadPath, fileName);

                // Copy file vào thư mục
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await cccdImage.CopyToAsync(stream);
                }

                // Lưu đường dẫn file vào Database (Cột CccdFrontImage đã tạo ở Bước 0)
                user.CccdFrontImage = "/uploads/" + fileName;
                user.IsKycVerified = true; // Đánh dấu là đã xác thực eKYC
                await _userManager.UpdateAsync(user);

                TempData["Success"] = "Tải ảnh Căn Cước Công Dân thành công!";
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn một file ảnh.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}