using Canteen.Data;
using Canteen.Models;
using Canteen.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// THÊM 2 DÒNG NÀY ĐỂ CẤU HÌNH SESSION
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giỏ hàng tồn tại 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 1. Cấu hình AppDbContext kết nối với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Identity (Bao gồm User và Role ?? sau này phân quyền Admin/Nhân viên)
builder.Services.AddIdentity<AppUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false; // Tạm tắt yêu cầu xác thực email cho dễ test
    options.Password.RequireNonAlphanumeric = false; // Tạm tắt yêu cầu mật khẩu phải có ký tự đặc biệt
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRazorPages();

// THÊM DÒNG NÀY ĐỂ SỬA LỖI IEmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login"; // Đường dẫn đến trang Đăng nhập
    options.LogoutPath = "/Identity/Account/Logout"; // Đường dẫn đến trang Đăng xuất
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Đường dẫn khi bị chặn quyền
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // <--- THÊM DÒNG NÀY Ở ĐÂY

app.UseAuthentication(); // Bắt buộc phải có dòng này trước Authorization
app.UseAuthorization();

// 1. Cấu hình Route cho Area (Admin) - Phải đặt trên Route mặc định
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. Cấu hình Route mặc định (Client)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 3. Map Razor Pages cho Identity (Giao diện đăng nhập/đăng ký)
app.MapRazorPages();

// --- ĐOẠN CODE TỰ ĐỘNG TẠO ROLE VÀ CẤP QUYỀN ADMIN ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    // 1. Tạo Role "Admin" và "Customer" nếu trong Database chưa có
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    if (!await roleManager.RoleExistsAsync("Customer"))
    {
        await roleManager.CreateAsync(new IdentityRole("Customer"));
    }
    // THÊM ĐOẠN NÀY ĐỂ KHỞI TẠO ROLE NHÂN VIÊN:
    if (!await roleManager.RoleExistsAsync("Staff"))
    {
        await roleManager.CreateAsync(new IdentityRole("Staff"));
    }

    // 2. Tìm tài khoản của bạn và gắn mác "Admin"
    // LƯU Ý: Thay đổi email dưới đây thành đúng email bạn đã đăng ký nếu bạn dùng email khác
    var adminUser = await userManager.FindByEmailAsync("11111@gmail.com");
    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
// ------------------------------------------------------

app.Run(); // Dòng này là dòng cuối cùng mặc định của file
