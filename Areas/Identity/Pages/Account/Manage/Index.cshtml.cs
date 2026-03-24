// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DoAnWebDemo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DoAnWebDemo.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Họ và Tên")]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập số CCCD")]
            [StringLength(12, MinimumLength = 9, ErrorMessage = "CCCD phải từ 9 đến 12 số")]
            [Display(Name = "Số Căn cước công dân")]
            public string CCCD { get; set; }

            [Display(Name = "Địa chỉ thường trú")]
            public string Address { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập thu nhập")]
            [Range(0, 1000000000, ErrorMessage = "Thu nhập không hợp lệ")]
            [Display(Name = "Thu nhập hàng tháng (VNĐ)")]
            public decimal MonthlyIncome { get; set; }

            public bool IsKycVerified { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            Username = userName;

            Input = new InputModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CCCD = user.CCCD,
                Address = user.Address,
                MonthlyIncome = user.MonthlyIncome,
                IsKycVerified = user.IsKycVerified
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // CẬP NHẬT CÁC TRƯỜNG THÔNG TIN MỚI VÀO CSDL
            user.FullName = Input.FullName;
            user.PhoneNumber = Input.PhoneNumber;
            user.CCCD = Input.CCCD;
            user.Address = Input.Address;
            user.MonthlyIncome = Input.MonthlyIncome;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Lỗi: Không thể cập nhật hồ sơ.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Hồ sơ của bạn đã được cập nhật thành công.";
            return RedirectToPage();
        }
    }
}