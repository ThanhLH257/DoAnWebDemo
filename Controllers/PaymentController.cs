using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebDemo.Controllers
{
    [Authorize] // Bắt buộc khách phải đăng nhập mới xem được nợ của mình
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // F7, F8 & F10: Hiển thị danh sách các khoản nợ của User đang đăng nhập
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Lấy các khoản vay đang hoạt động (Approved/Active) kèm lịch trả nợ
            var loans = await _context.LoanApplications
                .Include(l => l.Package)
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.UserId == userId && (l.Status == "Approved" || l.Status == "Active"))
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
                .ThenInclude(l => l.Package)
                .FirstOrDefaultAsync(s => s.Id == id);

            // BẢO MẬT: Kiểm tra tính hợp lệ của đơn vay
            if (schedule != null && schedule.LoanApplication.UserId == _userManager.GetUserId(User))
            {
                if (!schedule.IsPaid)
                {
                    var user = await _userManager.GetUserAsync(User);

                    schedule.IsPaid = true; // Cập nhật trạng thái Đã đóng tiền
                    schedule.PaidDate = DateTime.Now; // Lưu ngày đóng tiền thực tế

                    await _context.SaveChangesAsync();

                    // --- GỬI EMAIL BIÊN LAI XÁC NHẬN (Nâng cấp) ---
                    await _emailSender.SendEmailAsync(user.Email, "Biên lai thanh toán FastLoan #" + schedule.Id,
                        $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 12px; overflow: hidden;'>
                            <div style='background-color: #0d6efd; padding: 25px; text-align: center;'>
                                <h1 style='color: white; margin: 0;'>FastLoan VN</h1>
                                <p style='color: #e0f0ff; margin-top: 5px;'>Xác nhận giao dịch thành công</p>
                            </div>
                            <div style='padding: 30px; background-color: #ffffff;'>
                                <h2 style='color: #198754;'>Thanh Toán Thành Công!</h2>
                                <p>Xin chào <b>{user.FullName}</b>,</p>
                                <p>Chúng tôi đã ghi nhận khoản thanh toán cho kỳ hạn thứ <b>{schedule.InstallmentNumber}</b> của đơn vay <b>#{schedule.LoanApplicationId}</b>.</p>
                                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                    <table style='width: 100%; border-collapse: collapse;'>
                                        <tr><td style='padding: 5px 0;'>Số tiền:</td><td style='text-align: right; font-weight: bold; color: #dc3545;'>{schedule.TotalAmount:N0} VNĐ</td></tr>
                                        <tr><td style='padding: 5px 0;'>Ngày thanh toán:</td><td style='text-align: right;'>{schedule.PaidDate:dd/MM/yyyy HH:mm}</td></tr>
                                        <tr><td style='padding: 5px 0;'>Mã giao dịch:</td><td style='text-align: right;'>FAST-{schedule.Id}-{DateTime.Now.Ticks / 10000}</td></tr>
                                    </table>
                                </div>
                                <p style='font-size: 14px; color: #6c757d;'>Cảm ơn bạn đã tin dùng dịch vụ của chúng tôi. Bạn có thể xem chi tiết và in biên lai chính thức trên trang cá nhân.</p>
                            </div>
                            <div style='background-color: #f1f3f5; padding: 15px; text-align: center; font-size: 12px; color: #adb5bd;'>
                                © 2026 FastLoan FinTech. Bảo mật theo tiêu chuẩn PCI DSS.
                            </div>
                        </div>");

                    TempData["SuccessMessage"] = $"Thanh toán thành công kỳ {schedule.InstallmentNumber}! Biên lai đã được gửi về Email: {user.Email}";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // Action này dùng để khách hàng bấm "Gửi biên lai" trên giao diện
        [HttpPost]
        public async Task<IActionResult> SendEmailReceipt(int id)
        {
            var schedule = await _context.RepaymentSchedules
                .Include(s => s.LoanApplication)
                .ThenInclude(l => l.Package)
                .Include(s => s.LoanApplication.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            // Kiểm tra tính hợp lệ: Phải tồn tại, đã trả, và đúng là của User đang đăng nhập
            if (schedule != null && schedule.IsPaid && schedule.LoanApplication.UserId == _userManager.GetUserId(User))
            {
                var user = schedule.LoanApplication.User;

                // Tái sử dụng nội dung email chuyên nghiệp
                await _emailSender.SendEmailAsync(user.Email, "Biên lai thanh toán FastLoan #" + schedule.Id,
                    $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #198754; padding: 25px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>FastLoan VN</h1>
                    <p style='color: #e0f0ff; margin-top: 5px;'>Biên lai thanh toán điện tử</p>
                </div>
                <div style='padding: 30px; background-color: #ffffff;'>
                    <h2 style='color: #333;'>Xác nhận giao dịch</h2>
                    <p>Chào <b>{user.FullName}</b>, đây là biên lai cho khoản thanh toán kỳ {schedule.InstallmentNumber} của bạn.</p>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <table style='width: 100%;'>
                            <tr><td>Số tiền:</td><td style='text-align: right; font-weight: bold;'>{schedule.TotalAmount:N0} đ</td></tr>
                            <tr><td>Ngày trả:</td><td style='text-align: right;'>{schedule.PaidDate:dd/MM/yyyy}</td></tr>
                            <tr><td>Mã đơn vay:</td><td style='text-align: right;'>#{schedule.LoanApplicationId}</td></tr>
                        </table>
                    </div>
                    <p style='font-size: 13px; color: #666;'>Biên lai này có giá trị pháp lý thay thế cho chứng từ giấy.</p>
                </div>
            </div>");

                TempData["SuccessMessage"] = "Biên lai điện tử đã được gửi lại vào email của bạn!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}