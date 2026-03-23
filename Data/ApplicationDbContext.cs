using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebDemo.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // Thêm 3 dòng này để Entity Framework nhận diện bảng mới
        public DbSet<LoanPackage> LoanPackages { get; set; }
        public DbSet<LoanApplication> LoanApplications { get; set; }
        public DbSet<RepaymentSchedule> RepaymentSchedules { get; set; }
    }
}
