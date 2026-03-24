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

        // GET: LoanApplications
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.LoanApplications.Include(l => l.Package).Include(l => l.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LoanApplications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loanApplication = await _context.LoanApplications
                .Include(l => l.Package)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanApplication == null)
            {
                return NotFound();
            }

            return View(loanApplication);
        }

        // GET: LoanApplications/Create
        public IActionResult Create()
        {
            ViewData["PackageId"] = new SelectList(_context.LoanPackages, "Id", "PackageName");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: LoanApplications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PackageId,LoanAmount,TermMonths,Purpose")] LoanApplication loanApplication)
        {
            // 1. Tự động gán thông tin ngầm
            loanApplication.UserId = _userManager.GetUserId(User); // Lấy ID người đang đăng nhập
            loanApplication.Status = "Pending"; // Mặc định là chờ duyệt
            loanApplication.CreditScore = 0; // Tạm thời bằng 0 (Sẽ làm ở F5)
            loanApplication.CreatedAt = DateTime.Now;

            // 2. Xóa lỗi xác thực của các trường mà ta vừa tự gán ngầm
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User); // Lấy thông tin user

                loanApplication.UserId = user.Id;
                loanApplication.Status = "Pending";
                loanApplication.CreatedAt = DateTime.Now;

                // --- BẮT ĐẦU F5: THUẬT TOÁN CHẤM ĐIỂM ---
                // Ví dụ: Cứ mỗi 1 triệu VNĐ thu nhập sẽ được cộng 5 điểm tín dụng. Trừ đi số tháng vay.
                int calculatedScore = (int)(user.MonthlyIncome / 1000000) * 5 - loanApplication.TermMonths;
                loanApplication.CreditScore = calculatedScore > 0 ? calculatedScore : 10; // Tối thiểu 10 điểm
                                                                                          // --- KẾT THÚC F5 ---

                _context.Add(loanApplication);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PackageId"] = new SelectList(_context.LoanPackages, "Id", "PackageName", loanApplication.PackageId);
            return View(loanApplication);
        }

        // GET: LoanApplications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loanApplication = await _context.LoanApplications.FindAsync(id);
            if (loanApplication == null)
            {
                return NotFound();
            }
            ViewData["PackageId"] = new SelectList(_context.LoanPackages, "Id", "PackageName", loanApplication.PackageId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", loanApplication.UserId);
            return View(loanApplication);
        }

        // POST: LoanApplications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,PackageId,LoanAmount,TermMonths,Purpose,CreditScore,Status,CreatedAt")] LoanApplication loanApplication)
        {
            if (id != loanApplication.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loanApplication);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanApplicationExists(loanApplication.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PackageId"] = new SelectList(_context.LoanPackages, "Id", "PackageName", loanApplication.PackageId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", loanApplication.UserId);
            return View(loanApplication);
        }

        // GET: LoanApplications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loanApplication = await _context.LoanApplications
                .Include(l => l.Package)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanApplication == null)
            {
                return NotFound();
            }

            return View(loanApplication);
        }

        // POST: LoanApplications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loanApplication = await _context.LoanApplications.FindAsync(id);
            if (loanApplication != null)
            {
                _context.LoanApplications.Remove(loanApplication);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoanApplicationExists(int id)
        {
            return _context.LoanApplications.Any(e => e.Id == id);
        }
    }
}
