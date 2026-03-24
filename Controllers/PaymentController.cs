using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebDemo.Controllers
{
    [Authorize] // Bắt buộc khách phải đăng nhập mới xem được nợ của mình
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // F7 & F8: Hiển thị danh sách các khoản nợ của User đang đăng nhập
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var loans = await _context.LoanApplications
                .Include(l => l.Package)
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.UserId == userId && (l.Status == "Approved" || l.Status == "Active")) // Sửa dòng này để lấy cả Active
                .ToListAsync();

            // --- BẮT ĐẦU F10: TÌM CÁC KỲ SẮP ĐẾN HẠN (TRONG VÒNG 3 NGÀY) ---
            var upcomingDue = loans.SelectMany(l => l.RepaymentSchedules)
                .Count(s => !s.IsPaid && s.DueDate <= DateTime.Now.AddDays(3));

            if (upcomingDue > 0)
            {
                ViewBag.Notification = $"CẢNH BÁO: Bạn có {upcomingDue} kỳ thanh toán sắp đến hạn hoặc đã quá hạn. Vui lòng thanh toán ngay để tránh phí phạt!";
            }
            // --- KẾT THÚC F10 ---

            return View(loans);
        }

        // F10: Xử lý khi khách hàng bấm nút "Thanh Toán"
        [HttpPost]
        public async Task<IActionResult> PayInstallment(int id)
        {
            var schedule = await _context.RepaymentSchedules
                .Include(s => s.LoanApplication)
                .FirstOrDefaultAsync(s => s.Id == id);

            // BẢO MẬT: Kiểm tra xem lịch trả nợ này có đúng là của khách hàng đang đăng nhập không
            if (schedule != null && schedule.LoanApplication.UserId == _userManager.GetUserId(User))
            {
                if (!schedule.IsPaid)
                {
                    schedule.IsPaid = true; // Cập nhật trạng thái Đã đóng tiền
                    schedule.PaidDate = DateTime.Now; // Lưu ngày đóng tiền thực tế

                    await _context.SaveChangesAsync();

                    // Gửi thông báo thành công ra màn hình
                    TempData["SuccessMessage"] = $"Thanh toán thành công Kỳ {schedule.InstallmentNumber}!";
                }
            }
            return RedirectToAction(nameof(Index)); // Quay lại trang lịch nợ
        }
    }
}