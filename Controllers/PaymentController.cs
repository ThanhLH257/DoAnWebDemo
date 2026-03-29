using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        // F10: Xử lý khi khách hàng bấm nút "Thanh Toán" (Áp dụng cho cả Người vay và Người bảo lãnh)
        [HttpPost]
        public async Task<IActionResult> PayInstallment(int id)
        {
            // Lấy kỳ hạn kèm theo thông tin Gói vay và thông tin Người Vay (User)
            var schedule = await _context.RepaymentSchedules
                .Include(s => s.LoanApplication)
                .ThenInclude(l => l.Package)
                .Include(s => s.LoanApplication.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            var currentUser = await _userManager.GetUserAsync(User);

            if (schedule != null && currentUser != null)
            {
                // BẢO MẬT: Kiểm tra xem người đang thao tác có quyền thanh toán không?
                // Quyền 1: Là chính chủ người vay
                bool isBorrower = schedule.LoanApplication.UserId == currentUser.Id;
                // Quyền 2: Là người bảo lãnh VÀ đã ấn chấp nhận bảo lãnh khoản vay này
                bool isGuarantor = schedule.LoanApplication.GuarantorId == currentUser.Id && schedule.LoanApplication.GuaranteeStatus == "Accepted";

                if ((isBorrower || isGuarantor) && !schedule.IsPaid)
                {
                    schedule.IsPaid = true; // Cập nhật trạng thái Đã đóng tiền
                    schedule.PaidDate = DateTime.Now; // Lưu ngày đóng tiền thực tế

                    await _context.SaveChangesAsync();

                    // --- 1. GỬI EMAIL BIÊN LAI CHO NGƯỜI VỪA ĐÓNG TIỀN ---
                    string payerType = isGuarantor ? "Người bảo lãnh thanh toán thay" : "Khách hàng tự thanh toán";

                    await _emailSender.SendEmailAsync(currentUser.Email, $"Biên lai thanh toán FastLoan #{schedule.Id}",
                        $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 12px; overflow: hidden;'>
                            <div style='background-color: #0d6efd; padding: 25px; text-align: center;'>
                                <h1 style='color: white; margin: 0;'>FastLoan VN</h1>
                                <p style='color: #e0f0ff; margin-top: 5px;'>Xác nhận giao dịch thành công</p>
                            </div>
                            <div style='padding: 30px; background-color: #ffffff;'>
                                <h2 style='color: #198754;'>Thanh Toán Thành Công!</h2>
                                <p>Xin chào <b>{currentUser.FullName}</b>,</p>
                                <p>Chúng tôi đã ghi nhận khoản thanh toán cho kỳ hạn thứ <b>{schedule.InstallmentNumber}</b> của đơn vay <b>#{schedule.LoanApplicationId}</b>.</p>
                                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                    <table style='width: 100%; border-collapse: collapse;'>
                                        <tr><td style='padding: 5px 0;'>Hình thức:</td><td style='text-align: right; font-weight: bold;'>{payerType}</td></tr>
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

                    // --- 2. NẾU NGƯỜI BẢO LÃNH ĐÓNG THAY, BÁO TIN CHO NGƯỜI VAY BIẾT ---
                    if (isGuarantor && schedule.LoanApplication.User != null && !string.IsNullOrEmpty(schedule.LoanApplication.User.Email))
                    {
                        await _emailSender.SendEmailAsync(schedule.LoanApplication.User.Email, "Thông báo: Khoản nợ đã được thanh toán thay",
                            $@"<div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #198754; border-radius: 10px;'>
                                <h2 style='color: #198754;'>THÔNG BÁO TẤT TOÁN KỲ HẠN</h2>
                                <p>Xin chào {schedule.LoanApplication.User.FullName},</p>
                                <p>Kỳ hạn số <b>{schedule.InstallmentNumber}</b> của khoản vay #{schedule.LoanApplicationId} vừa được thanh toán thành công bởi người bảo lãnh <b>{currentUser.FullName}</b>.</p>
                                <p>Số tiền thanh toán: <b style='color: #dc3545;'>{schedule.TotalAmount:N0} VNĐ</b>.</p>
                                <p>Vui lòng sắp xếp tài chính để hoàn trả lại số tiền này cho người bảo lãnh của bạn.</p>
                               </div>"
                        );
                    }

                    TempData["SuccessMessage"] = $"Thanh toán thành công kỳ {schedule.InstallmentNumber}! Biên lai đã gửi về Email của bạn.";
                }
            }

            // Dùng thủ thuật này để đưa người dùng quay lại ĐÚNG TRANG họ vừa đứng 
            // (Người vay thì về trang Lịch trả nợ, Bảo lãnh thì về trang Lịch sử bảo lãnh)
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
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