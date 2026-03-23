using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWebDemo.Models
{
    public class LoanApplication
    {
        [Key]
        public int Id { get; set; }

        // Liên kết với người dùng (Khách hàng vay)
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Liên kết với gói vay
        [Required]
        [Display(Name = "Gói vay")]
        public int PackageId { get; set; }
        [ForeignKey("PackageId")]
        public LoanPackage? Package { get; set; }

        [Required]
        [Display(Name = "Số tiền muốn vay")]
        public decimal LoanAmount { get; set; }

        [Required]
        [Display(Name = "Kỳ hạn (Tháng)")]
        public int TermMonths { get; set; }

        [Required, StringLength(500)]
        [Display(Name = "Mục đích vay")]
        public string Purpose { get; set; } = string.Empty;

        [Display(Name = "Điểm tín dụng AI")]
        public int CreditScore { get; set; } = 0; // Phục vụ chức năng F5

        [Display(Name = "Trạng thái")]
        // Trạng thái: Pending (Chờ duyệt), Approved (Đã duyệt), Rejected (Từ chối), Active (Đang vay), Completed (Đã trả xong)
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}