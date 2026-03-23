using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DoAnWebDemo.Models
{
    // Kế thừa từ IdentityUser để lấy sẵn Email, Password, PhoneNumber...
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Địa chỉ thường trú")]
        [StringLength(255)]
        public string? Address { get; set; }

        [Display(Name = "Số Căn cước công dân")]
        [StringLength(12, MinimumLength = 9, ErrorMessage = "CCCD/CMND phải từ 9 đến 12 số")]
        public string? CCCD { get; set; }

        [Display(Name = "Thu nhập hàng tháng (VNĐ)")]
        // Dùng decimal cho tiền tệ. Thu nhập dùng để tính điểm AI Scoring (F5)
        public decimal MonthlyIncome { get; set; } = 0;

        [Display(Name = "Trạng thái xác thực eKYC")]
        // Mặc định khách mới đăng ký là chưa xác thực (false)
        public bool IsKycVerified { get; set; } = false;

        [Display(Name = "Ảnh CCCD mặt trước")]
        // Lưu đường dẫn file ảnh khi upload (chức năng F9 của Thành viên 3)
        public string? CccdFrontImage { get; set; }

        // Ngày tạo tài khoản để Admin xem thống kê (F12)
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
