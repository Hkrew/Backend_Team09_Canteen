using Canteen.Data;
using Canteen.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Controllers
{
    // Cấp quyền: Chỉ Khách hàng mới được vào xem lịch sử
    [Authorize(Roles = "Customer")]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public OrdersController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // TRANG LỊCH SỬ ĐƠN HÀNG (/Orders/History)
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Tìm toàn bộ đơn hàng của riêng người này, sắp xếp từ mới nhất đến cũ nhất
            var orders = await _context.Orders
                .Where(o => o.AppUserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // TRANG CHI TIẾT ĐƠN HÀNG (/Orders/Details/5)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Tìm đơn hàng theo ID.
            // BẮT BUỘC phải kèm điều kiện AppUserId == user.Id để khách không thể "nhòm ngó" đơn của người khác
            var order = await _context.Orders
                .Include(o => o.OrderDetails)       // Kéo theo danh sách chi tiết đơn
                .ThenInclude(od => od.Product)      // Kéo theo thông tin món ăn trong chi tiết đó
                .FirstOrDefaultAsync(o => o.Id == id && o.AppUserId == user.Id);

            // (Lưu ý: Nếu thuộc tính ID trong Order.cs của bạn tên khác, hãy đổi AppUserId thành tên tương ứng giống hệt ở hàm History nhé)

            if (order == null) return NotFound();

            return View(order);
        }
    }
}