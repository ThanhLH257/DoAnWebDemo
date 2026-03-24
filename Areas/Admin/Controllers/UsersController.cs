using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebDemo.Models;

namespace DoAnWebDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // 1. DANH SÁCH TÀI KHOẢN (XEM)
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        // 2. FORM TẠO TÀI KHOẢN MỚI (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 3. XỬ LÝ TẠO TÀI KHOẢN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Email, string FullName, string Password, string PhoneNumber, string Role)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Email,
                    Email = Email,
                    FullName = FullName,
                    PhoneNumber = PhoneNumber,
                    EmailConfirmed = true, // Tự động xác nhận email do Admin tạo
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(Role))
                    {
                        await _userManager.AddToRoleAsync(user, Role);
                    }
                    TempData["Message"] = "Đã tạo tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View();
        }

        // 4. XÁC NHẬN XÓA TÀI KHOẢN (GET)
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // 5. XỬ LÝ XÓA TÀI KHOẢN (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Bảo mật: Không cho phép Admin tự xóa chính mình
                var currentUserId = _userManager.GetUserId(User);
                if (user.Id == currentUserId)
                {
                    TempData["Error"] = "Bạn không thể tự xóa tài khoản của chính mình đang đăng nhập!";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.DeleteAsync(user);
                TempData["Message"] = "Đã xóa tài khoản vĩnh viễn khỏi hệ thống!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}