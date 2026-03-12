using Canteen.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Canteen.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager; // Thêm công cụ quản lý phiên đăng nhập

        public ProfileController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // --- 1. HIỂN THỊ TRANG THÔNG TIN CÁ NHÂN ---
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(user);
        }

        // --- 2. XỬ LÝ LƯU THAY ĐỔI TOÀN DIỆN ---
        [HttpPost]
        public async Task<IActionResult> Index(string fullName, string phoneNumber, string email, string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            bool needsRelogin = false;

            // 1. Cập nhật Họ tên và Số điện thoại
            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            await _userManager.UpdateAsync(user);

            // 2. Cập nhật Email (Nếu khách hàng nhập email khác)
            if (!string.IsNullOrWhiteSpace(email) && email != user.Email)
            {
                // Kiểm tra xem email mới có bị trùng với người khác không
                var emailExists = await _userManager.FindByEmailAsync(email);
                if (emailExists != null && emailExists.Id != user.Id)
                {
                    TempData["Error"] = "Email này đã được sử dụng bởi một tài khoản khác!";
                    return View(user);
                }

                await _userManager.SetEmailAsync(user, email);
                await _userManager.SetUserNameAsync(user, email); // Username đồng bộ với Email
                needsRelogin = true;
            }

            // 3. Đổi Mật khẩu (Nếu khách có nhập mật khẩu mới)
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    TempData["Error"] = "Bạn phải nhập Mật khẩu hiện tại thì mới được đổi Mật khẩu mới!";
                    return View(user);
                }

                var passResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!passResult.Succeeded)
                {
                    TempData["Error"] = "Mật khẩu hiện tại không đúng, hoặc mật khẩu mới chưa đủ mạnh (cần chữ hoa, số, ký tự đặc biệt).";
                    return View(user);
                }
                needsRelogin = true;
            }

            // 4. Kiểm tra xem có cần đăng xuất không
            if (needsRelogin)
            {
                await _signInManager.SignOutAsync(); // Đăng xuất phiên hiện tại
                TempData["Success"] = "Cập nhật tài khoản thành công! Vui lòng đăng nhập lại với thông tin mới.";
                // Chuyển hướng về trang Đăng nhập của Identity
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return View(user);
        }
    }
}