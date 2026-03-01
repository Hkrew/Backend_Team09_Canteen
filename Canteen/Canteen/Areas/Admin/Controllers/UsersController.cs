using Canteen.Data;
using Canteen.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Bắt buộc là Admin mới được vào
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public UsersController(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // --- 1. HIỂN THỊ & PHÂN LOẠI TÀI KHOẢN (Vấn đề 3) ---
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var allUsers = await _userManager.Users.Where(u => u.Id != currentUserId).ToListAsync();

            var staffList = new List<AppUser>();
            var customerList = new List<AppUser>();

            // Phân loại tài khoản
            foreach (var user in allUsers)
            {
                if (await _userManager.IsInRoleAsync(user, "Staff"))
                {
                    staffList.Add(user);
                }
                else
                {
                    customerList.Add(user); // Mặc định là Khách hàng
                }
            }

            // Dùng ViewBag để truyền 2 danh sách ra ngoài View
            ViewBag.StaffList = staffList;
            ViewBag.CustomerList = customerList;

            return View();
        }

        // --- 2. THÊM TÀI KHOẢN NHÂN VIÊN (Vấn đề 1) ---
        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(string email, string fullName, string phoneNumber, string password)
        {
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                PhoneNumber = phoneNumber
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Cấp ngay quyền Staff cho tài khoản này
                await _userManager.AddToRoleAsync(user, "Staff");
                return RedirectToAction(nameof(Index));
            }

            // Nếu mật khẩu không đủ mạnh (chưa có chữ hoa, ký tự đặc biệt...)
            TempData["Error"] = "Tạo thất bại! Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt.";
            return View();
        }

        // --- 3. SỬA THÔNG TIN (Chỉ cho phép sửa Staff - Vấn đề 2) ---
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // CHẶN: Nếu là Khách hàng thì đá văng ra, không cho sửa
            if (await _userManager.IsInRoleAsync(user, "Customer") || !(await _userManager.IsInRoleAsync(user, "Staff")))
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa thông tin Khách hàng!";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, string fullName, string phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Bảo mật kép: Kiểm tra lại lần nữa khi Submit
                if (await _userManager.IsInRoleAsync(user, "Staff"))
                {
                    user.FullName = fullName;
                    user.PhoneNumber = phoneNumber;
                    await _userManager.UpdateAsync(user);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // --- 4. XÓA TÀI KHOẢN ---
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // BƯỚC QUAN TRỌNG: Phải xóa hết Đơn hàng của người này trước để tránh lỗi dữ liệu
                var userOrders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.AppUserId == id)
                    .ToListAsync();

                if (userOrders.Any())
                {
                    _context.Orders.RemoveRange(userOrders);
                    await _context.SaveChangesAsync();
                }

                // Sau đó mới xóa tài khoản
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}