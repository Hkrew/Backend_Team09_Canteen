using Canteen.Data;
using Canteen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Canteen.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        // Tiêm AppDbContext để truy xuất Database
        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách sản phẩm, kèm theo thông tin Danh mục của nó
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();

            // Truyền danh sách này ra ngoài giao diện (View)
            return View(products);
        }

        // Các hàm Privacy, Error mặc định bên dưới bạn cứ giữ nguyên nhé
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}