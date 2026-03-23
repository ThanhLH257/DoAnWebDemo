using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWebDemo.Models
{
    public class RepaymentSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LoanApplicationId { get; set; }
        [ForeignKey("LoanApplicationId")]
        public LoanApplication? LoanApplication { get; set; }

        [Display(Name = "Kỳ thứ")]
        public int InstallmentNumber { get; set; } // VD: Kỳ 1, Kỳ 2...

        [Display(Name = "Ngày đến hạn")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Tiền gốc")]
        public decimal PrincipalAmount { get; set; }

        [Display(Name = "Tiền lãi")]
        public decimal InterestAmount { get; set; }

        [Display(Name = "Tổng phải trả")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Đã thanh toán?")]
        public bool IsPaid { get; set; } = false;

        [Display(Name = "Ngày thanh toán thực tế")]
        public DateTime? PaidDate { get; set; }
    }
}