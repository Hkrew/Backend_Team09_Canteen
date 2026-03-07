using Canteen.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // 1. Thêm dòng này

namespace Canteen.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên món ăn không được để trống")]
        [MinLength(3, ErrorMessage = "Tên món ăn phải có ít nhất 3 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá tiền")]
        [Range(1000, 10000000, ErrorMessage = "Giá tiền phải từ 1,000 đến 10,000,000 VNĐ")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true; // Trạng thái: Còn hàng hay Hết hàng

        // Khóa ngoại liên kết với bảng Category
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [ValidateNever] // 2. Thêm dòng này để form thêm món ăn không bị lỗi
        public Category Category { get; set; }
    }
}