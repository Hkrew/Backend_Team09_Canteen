using Canteen.Models;
using System.ComponentModel.DataAnnotations;

namespace Canteen.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string AppUserId { get; set; } // Khóa ngoại liên kết với AppUser (Khách hàng)
        public AppUser AppUser { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        // Trạng thái đơn hàng: 0 - Chờ xác nhận, 1 - Đang chuẩn bị, 2 - Hoàn thành, 3 - Đã hủy
        public int Status { get; set; } = 0;

        // Mối quan hệ 1-N: 1 Đơn hàng có nhiều Chi tiết đơn hàng
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}