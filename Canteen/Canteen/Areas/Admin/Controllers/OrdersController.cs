using Canteen.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Staff")] // Cả Admin và Nhân viên đều được duyệt đơn // Bắt buộc đăng nhập để vào trang quản lý
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. HIỂN THỊ TẤT CẢ ĐƠN HÀNG CỦA HỆ THỐNG ---
        public async Task<IActionResult> Index()
        {
            // Lấy toàn bộ đơn hàng, kèm thông tin User đã đặt
            var orders = await _context.Orders
                .Include(o => o.AppUser)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // --- 2. XEM CHI TIẾT ĐỂ DUYỆT ĐƠN ---
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.AppUser)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // --- 3. HÀM CẬP NHẬT TRẠNG THÁI (F10) ---
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, int status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            // Cập nhật xong quay lại trang chi tiết
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // --- 4. HÀM XÓA VĨNH VIỄN ĐƠN HÀNG ---
        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            // Tìm đơn hàng và Include luôn chi tiết đơn hàng để EF Core tự động xóa sạch
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync(); // Xóa khỏi Database
            }

            // Xóa xong thì quay trở lại trang Danh sách đơn hàng
            return RedirectToAction(nameof(Index));
        }
    }
}