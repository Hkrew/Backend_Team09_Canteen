using Canteen.Data;
using Canteen.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Staff")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env; // Công cụ giúp xác định thư mục wwwroot để lưu ảnh

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // --- 1. DANH SÁCH ---
        // THÊM TÌM KIẾM VÀ PHÂN TRANG VÀO HÀM INDEX
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            // 1. Lấy toàn bộ dữ liệu (chưa chạy lệnh SQL vội)
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // 2. TÌM KIẾM: Nếu có từ khóa, lọc ra các món có tên chứa từ khóa đó
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Name.Contains(searchString));
            }

            // 3. PHÂN TRANG:
            int pageSize = 5; // Số lượng món ăn trên 1 trang (bạn có thể đổi thành 10 tùy ý)
            int pageIndex = pageNumber ?? 1; // Nếu không có số trang thì mặc định là trang 1

            // Đếm tổng số món (sau khi đã lọc) để tính ra tổng số trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Cắt lấy dữ liệu của trang hiện tại (Dùng Skip và Take)
            var products = await query
                .OrderByDescending(p => p.Id) // Món mới thêm sẽ lên đầu
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 4. Đẩy thông tin sang View để hiển thị thanh phân trang
            ViewBag.CurrentPage = pageIndex;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString; // Giữ lại từ khóa tìm kiếm trên ô input

            return View(products);
        }

        // --- 2. THÊM MỚI (GET) ---
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // --- 3. THÊM MỚI (POST) - CÓ XỬ LÝ ẢNH ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            // Bỏ qua kiểm tra lỗi của Category ảo để tránh bị false validation
            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl"); // <--- BỔ SUNG DÒNG NÀY

            if (ModelState.IsValid)
            {
                // Xử lý lưu File Ảnh nếu có
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Tạo tên file ngẫu nhiên để không bị trùng (VD: sdfsd-abc.jpg)
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var uploadPath = Path.Combine(_env.WebRootPath, "images"); // Trỏ vào wwwroot/images

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath); // Nếu chưa có mục images thì tạo

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Lưu đường dẫn vào database
                    product.ImageUrl = "/images/" + fileName;
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // --- 4. CẬP NHẬT (GET) ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // --- 5. CẬP NHẬT (POST) - CÓ XỬ LÝ ẢNH ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.Id) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl");

            // BƯỚC 2: THÊM DÒNG NÀY ĐỂ BÁO HỆ THỐNG BỎ QUA KIỂM TRA FILE ẢNH
            ModelState.Remove("imageFile");

            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null) return NotFound();

                // Chỉ cập nhật thông tin chữ
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.IsAvailable = product.IsAvailable;

                // Xử lý ảnh (NẾU CÓ)
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var uploadPath = Path.Combine(_env.WebRootPath, "images");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    existingProduct.ImageUrl = "/images/" + fileName;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // --- 6. XÓA ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}