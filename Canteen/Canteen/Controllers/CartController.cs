using Canteen.Data;
using Canteen.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json; // Thư viện dùng để ép kiểu Session
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Canteen.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        // 1. THÊM DÒNG NÀY ĐỂ KHAI BÁO _userManager
        private readonly UserManager<AppUser> _userManager;

        // 2. SỬA LẠI HÀM KHỞI TẠO NÀY (Thêm tham số UserManager)
        public CartController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager; // 3. Gán giá trị
        }

        // --- 1. HÀM HỖ TRỢ ĐỌC/GHI SESSION ---
        private List<CartItem> GetCartItems()
        {
            var sessionCart = HttpContext.Session.GetString("Cart");
            if (sessionCart == null) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(sessionCart);
        }

        private void SaveCartSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        // --- 2. CHỨC NĂNG THÊM VÀO GIỎ HÀNG ---
        public IActionResult AddToCart(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == id);

            if (item != null)
            {
                item.Quantity++; // Nếu món đã có trong giỏ -> Tăng số lượng
            }
            else
            {
                // Nếu món chưa có -> Thêm mới vào giỏ
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl
                });
            }

            SaveCartSession(cart);
            return RedirectToAction("Index", "Home"); // Thêm xong quay lại trang chủ
        }

        // --- 3. HÀM HIỂN THỊ TRANG GIỎ HÀNG ---
        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        // --- 4. HÀM TĂNG SỐ LƯỢNG ---
        public IActionResult Increase(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                item.Quantity++;
                SaveCartSession(cart);
            }
            return RedirectToAction("Index");
        }

        // --- 5. HÀM GIẢM SỐ LƯỢNG ---
        public IActionResult Decrease(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    cart.Remove(item); // Nếu số lượng giảm xuống 0 thì xóa luôn
                }
                SaveCartSession(cart);
            }
            return RedirectToAction("Index");
        }

        // --- 6. HÀM XÓA MÓN ĂN KHỎI GIỎ ---
        public IActionResult Remove(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartSession(cart);
            }
            return RedirectToAction("Index");
        }

        // --- 7. HÀM THANH TOÁN (LƯU VÀO DATABASE) ---
        [Authorize] // Bắt buộc phải đăng nhập mới được mua hàng
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (cart.Count == 0) return RedirectToAction("Index");

            // Lấy thông tin user đang đăng nhập
            var user = await _userManager.GetUserAsync(User);

            // 1. Tạo Đơn hàng mới
            var order = new Order
            {
                AppUserId = user.Id,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(c => c.Total),
                Status = 0 // Trạng thái: 0 - Chờ xác nhận
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // SQL Server tự sinh ra order.Id

            // 2. Lưu Chi tiết đơn hàng
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                };
                _context.OrderDetails.Add(orderDetail);
            }
            await _context.SaveChangesAsync();

            // 3. Xóa giỏ hàng sau khi đặt thành công
            HttpContext.Session.Remove("Cart");

            // 4. Chuyển sang trang Thành công, gửi kèm ID đơn hàng vừa tạo
            return RedirectToAction("CheckoutSuccess", new { id = order.Id });
        }

        // --- 8. TRANG THÀNH CÔNG VÀ HIỂN THỊ MÃ QR ---
        [Authorize]
        public IActionResult CheckoutSuccess(int id)
        {
            // Tìm lại đơn hàng vừa tạo để lấy tổng tiền hiển thị ra QR
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            return View(order); // Truyền dữ liệu đơn hàng ra View
        }
    }
}