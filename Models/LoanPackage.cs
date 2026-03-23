using System.ComponentModel.DataAnnotations;

namespace DoAnWebDemo.Models
{
    public class LoanPackage
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tên gói vay")]
        public string PackageName { get; set; } = string.Empty;

        [Display(Name = "Lãi suất (%/tháng)")]
        public double InterestRate { get; set; }

        [Display(Name = "Hạn mức tối thiểu")]
        public decimal MinAmount { get; set; }

        [Display(Name = "Hạn mức tối đa")]
        public decimal MaxAmount { get; set; }

        [Display(Name = "Kỳ hạn mặc định (Tháng)")]
        public int DefaultTermMonths { get; set; }
    }
}
