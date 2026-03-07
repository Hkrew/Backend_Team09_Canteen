using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Canteen.Models
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; }

        // IdentityUser đã có sẵn Id, UserName, Email, PhoneNumber, PasswordHash... nên không cần viết lại.
    }
}