using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Mở ra khi bạn đã cấu hình xong phân quyền
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Tính tổng quan con số
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalLoanApps = await _context.LoanApplications.CountAsync();

            // Tính tổng tiền đã giải ngân (Chỉ tính các đơn Approved)
            ViewBag.TotalDisbursed = await _context.LoanApplications
                .Where(l => l.Status == "Approved")
                .SumAsync(l => l.LoanAmount);

            // 2. Lấy dữ liệu vẽ Biểu đồ tròn (Tỷ lệ duyệt đơn)
            ViewBag.PendingCount = await _context.LoanApplications.CountAsync(l => l.Status == "Pending");
            ViewBag.ApprovedCount = await _context.LoanApplications.CountAsync(l => l.Status == "Approved");
            ViewBag.RejectedCount = await _context.LoanApplications.CountAsync(l => l.Status == "Rejected");

            return View();
        }
    }
}