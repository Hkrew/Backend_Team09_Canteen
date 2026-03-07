using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // 1. Thêm dòng này

namespace Canteen.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(50)]
        public string Name { get; set; }

        public string Description { get; set; }

        // Mối quan hệ 1-N: 1 danh mục có nhiều sản phẩm
        [ValidateNever] // 2. Thêm dòng này để bỏ qua lỗi bắt buộc nhập
        public ICollection<Product> Products { get; set; }
    }
}