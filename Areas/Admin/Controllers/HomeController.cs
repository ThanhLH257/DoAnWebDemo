using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DoAnWebDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Thống kê tổng quát (Widgets)
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalLoanApps = await _context.LoanApplications.CountAsync();
            ViewBag.TotalDisbursed = await _context.LoanApplications
                .Where(l => l.Status == "Active" || l.Status == "Approved")
                .SumAsync(l => l.LoanAmount);
            ViewBag.TotalInterest = ViewBag.TotalDisbursed * 0.15m; // Giả định lãi dự kiến 15%

            // 2. Dữ liệu Biểu đồ Tròn: Trạng thái đơn vay
            ViewBag.PendingCount = await _context.LoanApplications.CountAsync(l => l.Status == "Pending");
            ViewBag.ApprovedCount = await _context.LoanApplications.CountAsync(l => l.Status == "Approved" || l.Status == "Active");
            ViewBag.RejectedCount = await _context.LoanApplications.CountAsync(l => l.Status == "Rejected");

            // 3. Dữ liệu Biểu đồ Cột: Sự phổ biến của các gói vay
            var packageStats = await _context.LoanApplications
                .Include(l => l.Package)
                .GroupBy(l => l.Package.PackageName)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.PackageLabels = packageStats.Select(s => s.Name).ToArray();
            ViewBag.PackageData = packageStats.Select(s => s.Count).ToArray();

            // 4. Lấy 5 đơn vay mới nhất để hiển thị bảng nhanh
            var recentLoans = await _context.LoanApplications
                .Include(l => l.User)
                .Include(l => l.Package)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(recentLoans);
        }
    }
}