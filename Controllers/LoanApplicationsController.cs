using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace DoAnWebDemo.Controllers
{
    [Authorize]
    public class LoanApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoanApplicationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: LoanApplications - Danh sách đơn vay
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                var allLoans = _context.LoanApplications
                    .Include(l => l.Package)
                    .Include(l => l.User)
                    .OrderByDescending(l => l.CreatedAt);
                return View(await allLoans.ToListAsync());
            }

            var userId = _userManager.GetUserId(User);
            var myLoans = _context.LoanApplications
                .Include(l => l.Package)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt);

            return View(await myLoans.ToListAsync());
        }

        // GET: LoanApplications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var loanApplication = await _context.LoanApplications
                .Include(l => l.Package)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (loanApplication == null) return NotFound();

            if (!User.IsInRole("Admin") && loanApplication.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(loanApplication);
        }

        // GET: LoanApplications/Create - Trang đăng ký (Thiết kế mới)
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null ||
                string.IsNullOrEmpty(user.FullName) ||
                string.IsNullOrEmpty(user.CCCD) ||
                string.IsNullOrEmpty(user.Address) ||
                user.MonthlyIncome <= 0 ||
                user.IsKycVerified == false)
            {
                TempData["StatusMessage"] = "Error: Bạn cần hoàn thiện Họ tên, CCCD, Địa chỉ, Thu nhập và xác thực eKYC trước khi nộp đơn vay.";
                return RedirectToPage("/Account/Manage/Index", new { area = "Identity" });
            }

            var packages = await _context.LoanPackages.ToListAsync();

            // Cung cấp dữ liệu JSON để giao diện cập nhật ngay lập tức
            ViewBag.PackageData = packages.ToDictionary(p => p.Id.ToString(), p => new {
                name = p.PackageName,
                rate = p.InterestRate,
                min = p.MinAmount,
                max = p.MaxAmount,
                term = p.DefaultTermMonths
            });

            ViewData["PackageId"] = new SelectList(packages, "Id", "PackageName");

            return View();
        }

        // POST: LoanApplications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PackageId,LoanAmount,TermMonths,Purpose")] LoanApplication loanApplication)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                loanApplication.UserId = user.Id;
                loanApplication.Status = "Pending";
                loanApplication.CreatedAt = DateTime.Now;

                // Thuật toán chấm điểm AI dựa trên thu nhập và kỳ hạn
                int calculatedScore = (int)(user.MonthlyIncome / 1000000) * 10 - loanApplication.TermMonths;
                if (calculatedScore < 10) calculatedScore = 10;
                if (calculatedScore > 990) calculatedScore = 990;

                loanApplication.CreditScore = calculatedScore;

                _context.Add(loanApplication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Hồ sơ vay đã được gửi thành công. AI đang thẩm định kết quả!";
                return RedirectToAction(nameof(Index));
            }

            // Nạp lại dữ liệu nếu có lỗi
            var packages = await _context.LoanPackages.ToListAsync();
            ViewBag.PackageData = packages.ToDictionary(p => p.Id.ToString(), p => new {
                name = p.PackageName,
                rate = p.InterestRate,
                min = p.MinAmount,
                max = p.MaxAmount,
                term = p.DefaultTermMonths
            });
            ViewData["PackageId"] = new SelectList(packages, "Id", "PackageName", loanApplication.PackageId);

            return View(loanApplication);
        }

        // Các hàm hỗ trợ khác giữ nguyên logic cũ
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var loanApplication = await _context.LoanApplications.FindAsync(id);
            if (loanApplication == null) return NotFound();
            if (!User.IsInRole("Admin") && loanApplication.Status != "Pending") return RedirectToAction(nameof(Details), new { id = loanApplication.Id });
            ViewData["PackageId"] = new SelectList(_context.LoanPackages, "Id", "PackageName", loanApplication.PackageId);
            return View(loanApplication);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,PackageId,LoanAmount,TermMonths,Purpose,CreditScore,Status,CreatedAt")] LoanApplication loanApplication)
        {
            if (id != loanApplication.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try { _context.Update(loanApplication); await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException) { if (!LoanApplicationExists(loanApplication.Id)) return NotFound(); else throw; }
                return RedirectToAction(nameof(Index));
            }
            return View(loanApplication);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var loanApplication = await _context.LoanApplications.Include(l => l.Package).Include(l => l.User).FirstOrDefaultAsync(m => m.Id == id);
            if (loanApplication == null) return NotFound();
            if (!User.IsInRole("Admin") && loanApplication.Status != "Pending") return RedirectToAction(nameof(Index));
            return View(loanApplication);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loanApplication = await _context.LoanApplications.FindAsync(id);
            if (loanApplication != null)
            {
                if (!User.IsInRole("Admin") && loanApplication.Status != "Pending") return BadRequest("Không thể xóa hồ sơ.");
                _context.LoanApplications.Remove(loanApplication);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool LoanApplicationExists(int id) => _context.LoanApplications.Any(e => e.Id == id);
    }
}