using DoAnWebDemo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebDemo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LoanApproveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public LoanApproveController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // 1. Hiển thị danh sách đơn vay
        public async Task<IActionResult> Index()
        {
            var loanApplications = await _context.LoanApplications
                .Include(l => l.Package)
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return View(loanApplications);
        }

        // 2. Duyệt đơn
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var loan = await _context.LoanApplications.FindAsync(id);
            if (loan != null && loan.Status == "Pending")
            {
                loan.Status = "Approved";
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã phê duyệt hồ sơ #" + id;
            }
            return RedirectToAction(nameof(Index));
        }

        // 3. Từ chối
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var loan = await _context.LoanApplications.FindAsync(id);
            if (loan != null && loan.Status == "Pending")
            {
                loan.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã từ chối hồ sơ #" + id;
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. Giải ngân và tự động sinh Lịch trả nợ
        [HttpPost]
        public async Task<IActionResult> Disburse(int id)
        {
            var loan = await _context.LoanApplications
                .Include(l => l.Package)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan != null && loan.Status == "Approved")
            {
                loan.Status = "Active";

                // ĐỔI DOUBLE THÀNH DECIMAL VÀ ÉP KIỂU LÃI SUẤT
                decimal monthlyInterestRate = (decimal)(loan.Package.InterestRate / 100);
                decimal principalPerMonth = loan.LoanAmount / loan.TermMonths;
                decimal interestPerMonth = loan.LoanAmount * monthlyInterestRate;

                for (int i = 1; i <= loan.TermMonths; i++)
                {
                    var schedule = new DoAnWebDemo.Models.RepaymentSchedule
                    {
                        LoanApplicationId = loan.Id,
                        InstallmentNumber = i,
                        DueDate = DateTime.Now.AddMonths(i),
                        PrincipalAmount = principalPerMonth,
                        InterestAmount = interestPerMonth,
                        TotalAmount = principalPerMonth + interestPerMonth,
                        IsPaid = false
                    };
                    _context.RepaymentSchedules.Add(schedule);
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã giải ngân và tạo lịch trả nợ cho hồ sơ #" + id;
            }
            return RedirectToAction(nameof(Index));
        }

        // F11: Công cụ Quét và Phạt trễ hạn (TỐI ƯU HÓA HIỆU SUẤT)
        [HttpPost]
        public async Task<IActionResult> ApplyPenalty()
        {
            var overdueSchedules = await _context.RepaymentSchedules
                .Include(s => s.LoanApplication).ThenInclude(l => l.User)
                .Include(s => s.LoanApplication).ThenInclude(l => l.Guarantor)
                .Where(s => !s.IsPaid && s.DueDate < DateTime.Now)
                .ToListAsync();

            int count = 0;

            // TẠO MỘT DANH SÁCH CHỨA CÁC TÁC VỤ GỬI MAIL (Chưa chạy ngay)
            var emailTasks = new List<Task>();

            foreach (var schedule in overdueSchedules)
            {
                schedule.TotalAmount += 50000;
                count++;

                var loan = schedule.LoanApplication;

                // 1. CHUẨN BỊ MAIL CHO NGƯỜI VAY
                if (loan.User != null && !string.IsNullOrEmpty(loan.User.Email))
                {
                    // LƯU Ý: Không dùng 'await' ở đây, mà add thẳng Task vào danh sách
                    emailTasks.Add(_emailSender.SendEmailAsync(loan.User.Email, "CẢNH BÁO KHẨN: NỢ QUÁ HẠN & BỊ PHẠT",
                        $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #dc3545; border-radius: 10px; background-color: #fff5f5;'>
                            <h2 style='color: #dc3545; text-align: center;'>⚠️ THÔNG BÁO PHẠT TRỄ HẠN</h2>
                            <p>Kính gửi {loan.User.FullName},</p>
                            <p>Hệ thống FastLoan VN thông báo khoản vay <b>#{loan.Id}</b> của bạn đã <b>QUÁ HẠN THANH TOÁN</b> và bị tính thêm phí phạt.</p>
                            <ul>
                                <li>Kỳ hạn thứ: {schedule.InstallmentNumber}</li>
                                <li>Ngày đến hạn: {schedule.DueDate:dd/MM/yyyy}</li>
                                <li>Tổng tiền phải đóng (đã cộng 50.000đ phí phạt): <b style='color:red; font-size:18px;'>{schedule.TotalAmount:N0} VNĐ</b></li>
                            </ul>
                            <p>Vui lòng đăng nhập và thanh toán ngay lập tức để tránh phát sinh thêm phí phạt và bị ghi nhận nợ xấu trên hệ thống Trung tâm Thông tin Tín dụng (CIC).</p>
                            <p>Trân trọng,<br/>Ban Quản Trị Hệ Thống FastLoan VN.</p>
                        </div>"));
                }

                // 2. CHUẨN BỊ MAIL CHO NGƯỜI BẢO LÃNH
                if (loan.GuaranteeStatus == "Accepted" && loan.Guarantor != null && !string.IsNullOrEmpty(loan.GuarantorEmail))
                {
                    // LƯU Ý: Không dùng 'await' ở đây
                    emailTasks.Add(_emailSender.SendEmailAsync(loan.GuarantorEmail, "CẢNH BÁO KHẨN: NỢ QUÁ HẠN KHOẢN VAY BẢO LÃNH",
                        $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #dc3545; border-radius: 10px; background-color: #fff5f5;'>
                            <h2 style='color: #dc3545; text-align: center;'>⚠️ CẢNH BÁO TRỄ NỢ</h2>
                            <p>Kính gửi {loan.Guarantor.FullName},</p>
                            <p>Chúng tôi thông báo khoản vay <b>#{loan.Id}</b> của khách hàng <b>{loan.User.FullName}</b> mà bạn đang bảo lãnh đã <b>QUÁ HẠN THANH TOÁN</b>.</p>
                            <ul>
                                <li>Kỳ hạn thứ: {schedule.InstallmentNumber}</li>
                                <li>Ngày đến hạn: {schedule.DueDate:dd/MM/yyyy}</li>
                                <li>Tiền nợ (đã cộng phí phạt): <b style='color:red; font-size:18px;'>{schedule.TotalAmount:N0} VNĐ</b></li>
                            </ul>
                            <p>Vì bạn là Người bảo lãnh hợp pháp, xin vui lòng đôn đốc khách hàng thanh toán ngay lập tức. Nếu tình trạng này kéo dài, lịch sử tín dụng (CIC) của bạn cũng sẽ bị ảnh hưởng.</p>
                            <p>Trân trọng,<br/>Ban Quản Trị Hệ Thống FastLoan VN.</p>
                        </div>"));
                }
            }

            // Lưu tiền phạt vào database trước
            await _context.SaveChangesAsync();

            // THỰC THI GỬI TOÀN BỘ EMAIL CÙNG MỘT LÚC (SONG SONG)
            if (emailTasks.Any())
            {
                await Task.WhenAll(emailTasks);
            }

            TempData["Message"] = $"Đã quét và cộng tiền phạt cho {count} kỳ hạn trễ nợ! Đã gửi Email cảnh báo.";
            return RedirectToAction(nameof(Index));
        }
    }
}