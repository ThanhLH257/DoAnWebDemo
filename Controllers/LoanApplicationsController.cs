using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using DoAnWebDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnWebDemo.Controllers
{
    [Authorize]
    public class LoanApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public LoanApplicationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender; 
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
        // ĐÃ THÊM trường GuarantorEmail vào Bind
        public async Task<IActionResult> Create([Bind("PackageId,LoanAmount,TermMonths,Purpose,GuarantorEmail")] LoanApplication loanApplication)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Status");
            ModelState.Remove("GuaranteeStatus");

            // KIỂM TRA NGƯỜI BẢO LÃNH
            if (!string.IsNullOrEmpty(loanApplication.GuarantorEmail))
            {
                var guarantor = await _userManager.FindByEmailAsync(loanApplication.GuarantorEmail);
                if (guarantor == null)
                {
                    ModelState.AddModelError("GuarantorEmail", "Email người bảo lãnh chưa đăng ký tài khoản trên hệ thống!");
                }
                else if (guarantor.Id == user.Id)
                {
                    ModelState.AddModelError("GuarantorEmail", "Bạn không thể tự bảo lãnh cho chính mình!");
                }
                else
                {
                    loanApplication.GuarantorId = guarantor.Id;
                    loanApplication.GuaranteeStatus = "Pending"; // Chuyển trạng thái chờ họ xác nhận

                    // Cấp luôn quyền Guarantor cho người dùng này nếu họ chưa có
                    if (!await _userManager.IsInRoleAsync(guarantor, "Guarantor"))
                    {
                        await _userManager.AddToRoleAsync(guarantor, "Guarantor");
                    }
                }
            }
            else
            {
                loanApplication.GuaranteeStatus = "None";
            }

            if (ModelState.IsValid)
            {
                loanApplication.UserId = user.Id;
                loanApplication.Status = "Pending";
                loanApplication.CreatedAt = DateTime.Now;

                // Thuật toán chấm điểm AI dựa trên thu nhập và kỳ hạn
                int calculatedScore = (int)(user.MonthlyIncome / 1000000) * 10 - loanApplication.TermMonths;

                // Thuật toán AI: Nếu có người bảo lãnh, cộng thêm 50 điểm tín dụng
                if (loanApplication.GuaranteeStatus == "Pending") calculatedScore += 50;

                if (calculatedScore < 10) calculatedScore = 10;
                if (calculatedScore > 990) calculatedScore = 990;

                loanApplication.CreditScore = calculatedScore;

                _context.Add(loanApplication);
                await _context.SaveChangesAsync();

                // === BẮT ĐẦU: GỬI MAIL CHO NGƯỜI BẢO LÃNH ===
                if (loanApplication.GuaranteeStatus == "Pending")
                {
                    string callbackUrl = Url.Action("GuaranteeRequests", "LoanApplications", null, Request.Scheme);
                    await _emailSender.SendEmailAsync(loanApplication.GuarantorEmail, "YÊU CẦU BẢO LÃNH KHOẢN VAY - FASTLOAN",
                        $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #0d6efd;'>FastLoan VN - Yêu cầu bảo lãnh</h2>
                            <p>Xin chào,</p>
                            <p>Khách hàng <b>{user.FullName}</b> ({user.PhoneNumber}) vừa chỉ định bạn làm người bảo lãnh cho khoản vay trị giá <b style='color:red;'>{loanApplication.LoanAmount:N0} VNĐ</b>.</p>
                            <p>Nếu bạn đồng ý, vui lòng đăng nhập vào hệ thống để xác nhận. Bạn sẽ phải chịu trách nhiệm tài chính nếu người vay không hoàn thành nghĩa vụ thanh toán.</p>
                            <a href='{callbackUrl}' style='display: inline-block; padding: 10px 20px; background-color: #198754; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>XEM VÀ XÁC NHẬN</a>
                        </div>");
                }
                // === KẾT THÚC GỬI MAIL ===

                TempData["SuccessMessage"] = "Hồ sơ đã được gửi! Đã gửi Email thông báo cho Người bảo lãnh.";
                return RedirectToAction(nameof(Index));

                TempData["SuccessMessage"] = "Hồ sơ đã được gửi! Nếu có người bảo lãnh, vui lòng nhắc họ đăng nhập để xác nhận.";
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

        // ==============================================================
        // PHẦN CHỨC NĂNG DÀNH CHO NGƯỜI BẢO LÃNH (GUARANTOR)
        // ==============================================================

        // 1. Hiển thị danh sách đơn đang nhờ bảo lãnh
        [Authorize]
        public async Task<IActionResult> GuaranteeRequests()
        {
            var userId = _userManager.GetUserId(User);
            var requests = await _context.LoanApplications
                .Include(l => l.User)
                .Include(l => l.Package)
                .Where(l => l.GuarantorId == userId && l.GuaranteeStatus == "Pending")
                .ToListAsync();

            return View(requests);
        }

        // 2. Chấp nhận bảo lãnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptGuarantee(int id)
        {
            var loan = await _context.LoanApplications.FindAsync(id);
            if (loan != null && loan.GuarantorId == _userManager.GetUserId(User))
            {
                loan.GuaranteeStatus = "Accepted";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bạn đã chấp nhận bảo lãnh cho khoản vay này!";
            }
            return RedirectToAction(nameof(GuaranteeRequests));
        }

        // 3. Từ chối bảo lãnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectGuarantee(int id)
        {
            var loan = await _context.LoanApplications.FindAsync(id);
            if (loan != null && loan.GuarantorId == _userManager.GetUserId(User))
            {
                loan.GuaranteeStatus = "Rejected";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bạn đã từ chối bảo lãnh.";
            }
            return RedirectToAction(nameof(GuaranteeRequests));
        }

        // Xem các đơn MÌNH ĐÃ CHẤP NHẬN BẢO LÃNH (Kèm theo dõi nợ)
        [Authorize(Roles = "Guarantor")]
        public async Task<IActionResult> GuaranteedHistory()
        {
            var userId = _userManager.GetUserId(User);
            var guaranteedLoans = await _context.LoanApplications
                .Include(l => l.User)
                .Include(l => l.Package)
                .Include(l => l.RepaymentSchedules) // Kéo theo Lịch trả nợ để biết người kia có trả không
                .Where(l => l.GuarantorId == userId && l.GuaranteeStatus == "Accepted")
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(guaranteedLoans);
        }


        // ==============================================================
        // CÁC CHỨC NĂNG HỖ TRỢ KHÁC (Edit, Delete, ...)
        // ==============================================================

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