using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;
using QuanLyThuVien.ViewModels;

namespace QuanLyThuVien.Controllers
{
    /// <summary>Hồ sơ cá nhân của bạn đọc: chỉ xem/sửa dữ liệu của chính mình (lấy từ Session, không nhận ID từ URL).</summary>
    [Auth(Roles = "3")]
    public class HoSoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HoSoController(ApplicationDbContext context) { _context = context; }

        private async Task<BanDoc?> GetMyBanDocAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            return await _context.BanDocs.Include(b => b.TaiKhoan).FirstOrDefaultAsync(b => b.MaTaiKhoan == userId);
        }

        private static HoSoPageViewModel Build(BanDoc b, HoSoViewModel? hs = null) => new()
        {
            BanDoc = b,
            TenDangNhap = b.TaiKhoan?.TenDangNhap ?? "",
            HoSo = hs ?? new HoSoViewModel
            {
                HoTen = b.HoTen, NgaySinh = b.NgaySinh, GioiTinh = b.GioiTinh,
                SoDienThoai = b.SoDienThoai, Email = b.Email, DiaChi = b.DiaChi
            }
        };

        public async Task<IActionResult> Index()
        {
            var b = await GetMyBanDocAsync();
            if (b == null)
            {
                TempData["Error"] = "Tài khoản chưa được liên kết với hồ sơ bạn đọc. Vui lòng liên hệ thủ thư.";
                return RedirectToAction("Index", "Home");
            }
            return View(Build(b));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhat([Bind(Prefix = "HoSo")] HoSoViewModel hs)
        {
            var b = await GetMyBanDocAsync();
            if (b == null) return NotFound();

            if (hs.NgaySinh.HasValue && hs.NgaySinh.Value.Date >= DateTime.Today)
                ModelState.AddModelError("HoSo.NgaySinh", "Ngày sinh phải nhỏ hơn ngày hiện tại.");

            if (!ModelState.IsValid) return View("Index", Build(b, hs));

            // Chỉ cập nhật các trường cá nhân — không bind NgayCapThe, HanThe, TrangThai, GhiChu
            b.HoTen = hs.HoTen; b.NgaySinh = hs.NgaySinh; b.GioiTinh = hs.GioiTinh;
            b.SoDienThoai = hs.SoDienThoai; b.Email = hs.Email; b.DiaChi = hs.DiaChi;
            if (b.TaiKhoan != null) { b.TaiKhoan.HoTen = hs.HoTen; b.TaiKhoan.Email = hs.Email; }
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", b.HoTen);
            TempData["Success"] = "Đã cập nhật hồ sơ cá nhân.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau([Bind(Prefix = "DoiMatKhau")] DoiMatKhauViewModel dm)
        {
            var b = await GetMyBanDocAsync();
            if (b?.TaiKhoan == null) return NotFound();

            if (ModelState.IsValid && b.TaiKhoan.MatKhau != dm.MatKhauCu)
                ModelState.AddModelError("DoiMatKhau.MatKhauCu", "Mật khẩu hiện tại không đúng.");

            if (!ModelState.IsValid)
            {
                var vm = Build(b);
                vm.DoiMatKhau = new DoiMatKhauViewModel();
                return View("Index", vm);
            }

            b.TaiKhoan.MatKhau = dm.MatKhauMoi;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã đổi mật khẩu.";
            return RedirectToAction(nameof(Index));
        }
    }
}
