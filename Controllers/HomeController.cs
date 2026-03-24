using DoAnWebDemo.Data; // Đảm bảo có namespace này để dùng ApplicationDbContext
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DoAnWebDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        // 1. THÊM: Khai báo DbContext để truy vấn dữ liệu gói vay
        private readonly ApplicationDbContext _context;

        // 2. CẬP NHẬT: Tiêm DbContext vào Constructor
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 3. CẬP NHẬT: Chuyển thành async Task và lấy 3 gói vay phổ biến
        public async Task<IActionResult> Index()
        {
            // Nếu người dùng đã đăng nhập VÀ có quyền Admin -> Vào thẳng trang quản trị
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            // Lấy 3 gói vay bất kỳ từ Database để hiển thị ra trang chủ
            // Bạn có thể thêm .OrderByDescending(p => p.InterestRate) nếu muốn lấy gói lãi cao nhất
            var packages = await _context.LoanPackages.Take(3).ToListAsync();

            // Truyền danh sách này sang View thông qua ViewBag
            ViewBag.PopularPackages = packages;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}