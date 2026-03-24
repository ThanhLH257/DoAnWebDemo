using DoAnWebDemo.Data;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebDemo.Areas.Admin.Controllers
{
    [Area("Admin")] // BẮT BUỘC: Đánh dấu Controller này thuộc khu vực Admin
    [Authorize(Roles = "Admin")] // Tạm thời ẩn dòng này đi để dễ Test. Khi nào phân quyền xong sẽ mở ra.
    public class LoanApproveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoanApproveController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách tất cả đơn vay
        public async Task<IActionResult> Index()
        {
            var loans = await _context.LoanApplications
                .Include(l => l.User)     // Lấy kèm thông tin Khách hàng (Họ tên)
                .Include(l => l.Package)  // Lấy kèm thông tin Gói vay
                .OrderByDescending(l => l.CreatedAt) // Đơn mới nhất lên đầu
                .ToListAsync();
            return View(loans);
        }

        // Xử lý khi Admin bấm nút DUYỆT
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            // Lấy đơn vay kèm theo thông tin Gói vay (để lấy lãi suất)
            var loan = await _context.LoanApplications
                .Include(l => l.Package)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan != null && loan.Status == "Pending")
            {
                loan.Status = "Approved"; // Đổi trạng thái thành Đã Duyệt

                // --- BẮT ĐẦU THUẬT TOÁN F7: SINH LỊCH TRẢ NỢ (EMI) ---
                double r = loan.Package.InterestRate / 100.0; // Lãi suất tháng (VD: 1.5% -> 0.015)
                int n = loan.TermMonths; // Số tháng vay
                decimal p = loan.LoanAmount; // Số tiền gốc

                // Tính số tiền phải trả đều mỗi tháng (Công thức EMI)
                decimal emi = 0;
                if (r > 0)
                {
                    double mathPower = Math.Pow(1 + r, n);
                    emi = (decimal)((double)p * r * mathPower / (mathPower - 1));
                }
                else
                {
                    emi = p / n; // Nếu lãi suất = 0
                }

                decimal remainingPrincipal = p; // Dư nợ ban đầu

                // Vòng lặp sinh ra N kỳ thanh toán
                for (int i = 1; i <= n; i++)
                {
                    // Tính tiền lãi tháng này = Dư nợ còn lại * lãi suất
                    decimal interestForMonth = (decimal)((double)remainingPrincipal * r);

                    // Tính tiền gốc tháng này = Tổng tiền tháng - Tiền lãi
                    decimal principalForMonth = emi - interestForMonth;

                    // Xử lý làm tròn kỳ cuối cùng để khớp 100% tiền gốc
                    if (i == n)
                    {
                        principalForMonth = remainingPrincipal;
                        emi = principalForMonth + interestForMonth;
                    }

                    // Tạo 1 dòng lịch trả nợ mới
                    var schedule = new RepaymentSchedule
                    {
                        LoanApplicationId = loan.Id,
                        InstallmentNumber = i, // Kỳ 1, 2, 3...
                        DueDate = DateTime.Now.AddMonths(i), // Hạn trả là tháng sau
                        PrincipalAmount = Math.Round(principalForMonth),
                        InterestAmount = Math.Round(interestForMonth),
                        TotalAmount = Math.Round(emi),
                        IsPaid = false
                    };

                    _context.RepaymentSchedules.Add(schedule);
                    remainingPrincipal -= principalForMonth; // Giảm dư nợ xuống
                }
                // --- KẾT THÚC THUẬT TOÁN ---

                await _context.SaveChangesAsync(); // Lưu tất cả vào Database cùng lúc
            }
            return RedirectToAction(nameof(Index));
        }

        // Xử lý khi Admin bấm nút TỪ CHỐI
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var loan = await _context.LoanApplications.FindAsync(id);
            if (loan != null && loan.Status == "Pending")
            {
                loan.Status = "Rejected"; // Đổi trạng thái thành Từ chối
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // F8: Nút Giải Ngân
        [HttpPost]
        public async Task<IActionResult> Disburse(int id)
        {
            var loan = await _context.LoanApplications.FindAsync(id);
            // Chỉ giải ngân cho đơn đã duyệt
            if (loan != null && loan.Status == "Approved")
            {
                loan.Status = "Active"; // Đổi trạng thái thành Đang Hoạt Động (Đã nhận tiền)
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã giải ngân tiền vào tài khoản khách hàng!";
            }
            return RedirectToAction(nameof(Index));
        }

        // F11: Công cụ Quét và Phạt trễ hạn
        [HttpPost]
        public async Task<IActionResult> ApplyPenalty()
        {
            // Tìm tất cả các kỳ chưa thanh toán VÀ đã quá hạn so với ngày hôm nay
            var overdueSchedules = await _context.RepaymentSchedules
                .Where(s => !s.IsPaid && s.DueDate < DateTime.Now)
                .ToListAsync();

            int count = 0;
            foreach (var schedule in overdueSchedules)
            {
                schedule.TotalAmount += 50000; // Phạt cứng 50.000đ/kỳ
                count++;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = $"Đã quét và cộng tiền phạt cho {count} kỳ hạn trễ nợ!";
            return RedirectToAction(nameof(Index));
        }
    }
}