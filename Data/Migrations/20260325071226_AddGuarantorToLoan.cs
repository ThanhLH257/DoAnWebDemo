using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnWebDemo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuarantorToLoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuaranteeStatus",
                table: "LoanApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuarantorEmail",
                table: "LoanApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuarantorId",
                table: "LoanApplications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_GuarantorId",
                table: "LoanApplications",
                column: "GuarantorId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplications_AspNetUsers_GuarantorId",
                table: "LoanApplications",
                column: "GuarantorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplications_AspNetUsers_GuarantorId",
                table: "LoanApplications");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplications_GuarantorId",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "GuaranteeStatus",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "GuarantorEmail",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "GuarantorId",
                table: "LoanApplications");
        }
    }
}
