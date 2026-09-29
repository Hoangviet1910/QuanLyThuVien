using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Controllers
{
    [Auth(Roles = "1,2")] // Admin và Thủ thư
    public class BanDocController : Controller
    {
        private const int PageSize = 10;
        private readonly ApplicationDbContext _context;

        public BanDocController(ApplicationDbContext context)
        {
            _context = context;
        }

        // theCon: "conhan" | "hethan" | "khoa"
        public async Task<IActionResult> Index(string? searchString, string? theCon, int? pageNumber)
        {
            var today = DateTime.Today;
            var query = _context.BanDocs.Include(b => b.TaiKhoan).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var key = searchString.Trim();
                query = query.Where(b => b.HoTen.Contains(key)
                    || (b.SoDienThoai != null && b.SoDienThoai.Contains(key))
                    || (b.Email != null && b.Email.Contains(key)));
            }

            query = theCon switch
            {
                "conhan" => query.Where(b => b.TrangThai && b.HanThe >= today),
                "hethan" => query.Where(b => b.HanThe < today),
                "khoa" => query.Where(b => !b.TrangThai),
                _ => query
            };

            int count = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
            int page = Math.Clamp(pageNumber ?? 1, 1, totalPages);

            var items = await query.OrderByDescending(b => b.MaBanDoc)
                .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentTheCon"] = theCon;
            ViewData["PageNumber"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = count;
            return View(items);
        }

        public IActionResult Create()
        {
            return View(new BanDoc { NgayCapThe = DateTime.Today, HanThe = DateTime.Today.AddYears(1), TrangThai = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanDoc banDoc, string? tenDangNhap, string? matKhau)
        {
            if (banDoc.NgaySinh.HasValue && banDoc.NgaySinh.Value.Date >= DateTime.Today)
                ModelState.AddModelError(nameof(BanDoc.NgaySinh), "Ngày sinh phải nhỏ hơn ngày hiện tại.");
            if (banDoc.HanThe.Date < banDoc.NgayCapThe.Date)
                ModelState.AddModelError(nameof(BanDoc.HanThe), "Hạn thẻ phải sau hoặc bằng ngày cấp thẻ.");

            // Tên đăng nhập: nhập tay, nếu bỏ trống thì dùng số điện thoại
            var username = string.IsNullOrWhiteSpace(tenDangNhap) ? banDoc.SoDienThoai?.Trim() : tenDangNhap.Trim();
            var password = string.IsNullOrWhiteSpace(matKhau) ? "123456" : matKhau;

            if (string.IsNullOrWhiteSpace(username))
                ModelState.AddModelError("TenDangNhap", "Nhập tên đăng nhập hoặc số điện thoại (dùng làm tên đăng nhập).");
            else if (username.Length > 50)
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập tối đa 50 ký tự.");
            else if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == username))
                ModelState.AddModelError("TenDangNhap", $"Tên đăng nhập '{username}' đã tồn tại.");

            if (!ModelState.IsValid)
            {
                ViewData["TenDangNhap"] = tenDangNhap;
                return View(banDoc);
            }

            // Gán navigation => EF insert TaiKhoan + BanDoc trong cùng một transaction
            banDoc.TaiKhoan = new TaiKhoan
            {
                TenDangNhap = username!,
                MatKhau = password,
                HoTen = banDoc.HoTen,
                Email = banDoc.Email,
                VaiTro = 3,
                TrangThai = banDoc.TrangThai
            };
            _context.BanDocs.Add(banDoc);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm bạn đọc '{banDoc.HoTen}'. Tài khoản đăng nhập: {username} / {password}";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var banDoc = await _context.BanDocs.Include(b => b.TaiKhoan).FirstOrDefaultAsync(b => b.MaBanDoc == id);
            if (banDoc == null) return NotFound();
            return View(banDoc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BanDoc model, string? matKhauMoi)
        {
            if (id != model.MaBanDoc) return NotFound();

            var banDoc = await _context.BanDocs.Include(b => b.TaiKhoan).FirstOrDefaultAsync(b => b.MaBanDoc == id);
            if (banDoc == null) return NotFound();

            if (model.NgaySinh.HasValue && model.NgaySinh.Value.Date >= DateTime.Today)
                ModelState.AddModelError(nameof(BanDoc.NgaySinh), "Ngày sinh phải nhỏ hơn ngày hiện tại.");
            if (model.HanThe.Date < model.NgayCapThe.Date)
                ModelState.AddModelError(nameof(BanDoc.HanThe), "Hạn thẻ phải sau hoặc bằng ngày cấp thẻ.");

            if (!ModelState.IsValid)
            {
                model.TaiKhoan = banDoc.TaiKhoan;
                return View(model);
            }

            banDoc.HoTen = model.HoTen;
            banDoc.NgaySinh = model.NgaySinh;
            banDoc.GioiTinh = model.GioiTinh;
            banDoc.SoDienThoai = model.SoDienThoai;
            banDoc.Email = model.Email;
            banDoc.DiaChi = model.DiaChi;
            banDoc.NgayCapThe = model.NgayCapThe;
            banDoc.HanThe = model.HanThe;
            banDoc.TrangThai = model.TrangThai;
            banDoc.GhiChu = model.GhiChu;

            if (banDoc.TaiKhoan != null)
            {
                banDoc.TaiKhoan.HoTen = model.HoTen;
                banDoc.TaiKhoan.Email = model.Email;
                banDoc.TaiKhoan.TrangThai = model.TrangThai; // khóa thẻ = khóa đăng nhập
                if (!string.IsNullOrWhiteSpace(matKhauMoi)) banDoc.TaiKhoan.MatKhau = matKhauMoi;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật bạn đọc '{banDoc.HoTen}'.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var banDoc = await _context.BanDocs.Include(b => b.TaiKhoan)
                .Include(b => b.PhieuMuons).ThenInclude(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                .FirstOrDefaultAsync(b => b.MaBanDoc == id);
            if (banDoc == null) return NotFound();
            return View(banDoc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var banDoc = await _context.BanDocs.Include(b => b.TaiKhoan).FirstOrDefaultAsync(b => b.MaBanDoc == id);
            if (banDoc == null) return NotFound();

            // Không xóa bạn đọc đã phát sinh phiếu mượn (giữ lịch sử)
            if (await _context.PhieuMuons.AnyAsync(p => p.MaBanDoc == id))
            {
                TempData["Error"] = $"Bạn đọc '{banDoc.HoTen}' đã có phiếu mượn nên không thể xóa. Hãy khóa thẻ thay vì xóa.";
                return RedirectToAction(nameof(Index));
            }
            var taiKhoan = banDoc.TaiKhoan;
            _context.BanDocs.Remove(banDoc);
            if (taiKhoan != null) _context.TaiKhoans.Remove(taiKhoan);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa bạn đọc '{banDoc.HoTen}'.";
            return RedirectToAction(nameof(Index));
        }
    }
}
