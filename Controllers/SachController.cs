using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;
using QuanLyThuVien.ViewModels;

namespace QuanLyThuVien.Controllers
{
    [Auth] // Các action quản trị gắn thêm [Auth(Roles = "1,2")]; TraCuu dành cho mọi tài khoản đăng nhập
    public class SachController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SachController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tìm kiếm + Lọc + Sắp xếp dùng chung cho Index (quản lý) và TraCuu (bạn đọc)
        private static IQueryable<Sach> ApplyFilters(IQueryable<Sach> sachs, SachQuery q)
        {
            if (!string.IsNullOrWhiteSpace(q.SearchString))
            {
                var key = q.SearchString.Trim();
                sachs = sachs.Where(s => s.TenSach.Contains(key)
                    || s.TheLoai.TenTheLoai.Contains(key)
                    || s.NhaXuatBan.TenNhaXuatBan.Contains(key));
            }
            if (q.TheLoaiId.HasValue) sachs = sachs.Where(s => s.MaTheLoai == q.TheLoaiId);
            if (q.NhaXuatBanId.HasValue) sachs = sachs.Where(s => s.MaNhaXuatBan == q.NhaXuatBanId);
            if (q.NamXuatBan.HasValue) sachs = sachs.Where(s => s.NamXuatBan == q.NamXuatBan);
            if (q.GiaTu.HasValue) sachs = sachs.Where(s => s.DonGia >= q.GiaTu.Value);
            if (q.GiaDen.HasValue) sachs = sachs.Where(s => s.DonGia <= q.GiaDen.Value);

            if (q.TrangThai == "hienthi") sachs = sachs.Where(s => s.TrangThai);
            else if (q.TrangThai == "khoa") sachs = sachs.Where(s => !s.TrangThai);

            if (q.TinhTrang == "con") sachs = sachs.Where(s => s.TrangThai && s.SoLuongCon > 0);
            else if (q.TinhTrang == "het") sachs = sachs.Where(s => !s.TrangThai || s.SoLuongCon <= 0);

            return q.SortOrder switch
            {
                "name_desc" => sachs.OrderByDescending(s => s.TenSach),
                "year" => sachs.OrderBy(s => s.NamXuatBan).ThenBy(s => s.TenSach),
                "year_desc" => sachs.OrderByDescending(s => s.NamXuatBan).ThenBy(s => s.TenSach),
                "price" => sachs.OrderBy(s => s.DonGia).ThenBy(s => s.TenSach),
                "price_desc" => sachs.OrderByDescending(s => s.DonGia).ThenBy(s => s.TenSach),
                "con" => sachs.OrderBy(s => s.SoLuongCon).ThenBy(s => s.TenSach),
                "con_desc" => sachs.OrderByDescending(s => s.SoLuongCon).ThenBy(s => s.TenSach),
                _ => sachs.OrderBy(s => s.TenSach),
            };
        }

        private async Task<(List<Sach> Items, int Page, int TotalPages, int Count)> PageAsync(IQueryable<Sach> sachs, int? pageNumber, int pageSize)
        {
            int count = await sachs.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
            int page = Math.Clamp(pageNumber ?? 1, 1, totalPages);
            var items = await sachs.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(); // phân trang trên truy vấn
            return (items, page, totalPages, count);
        }

        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Index([FromQuery] SachQuery q)
        {
            ViewData["TheLoaiId"] = new SelectList(_context.TheLoais.OrderBy(t => t.ThuTuHienThi), "MaTheLoai", "TenTheLoai", q.TheLoaiId);
            ViewData["NhaXuatBanId"] = new SelectList(_context.NhaXuatBans.OrderBy(n => n.TenNhaXuatBan), "MaNhaXuatBan", "TenNhaXuatBan", q.NhaXuatBanId);

            var query = ApplyFilters(_context.Sachs.Include(s => s.TheLoai).Include(s => s.NhaXuatBan), q);
            var r = await PageAsync(query, q.PageNumber, 10);

            ViewData["PageNumber"] = r.Page;
            ViewData["TotalPages"] = r.TotalPages;
            ViewData["TotalCount"] = r.Count;
            ViewData["Query"] = q;
            return View(r.Items);
        }

        // Tra cứu sách (bạn đọc xem danh mục + nút "Mượn sách")
        public async Task<IActionResult> TraCuu([FromQuery] SachQuery q)
        {
            q.NhaXuatBanId = null; q.TrangThai = null; // bạn đọc chỉ thấy sách đang hiển thị
            ViewData["TheLoaiId"] = new SelectList(_context.TheLoais.Where(t => t.TrangThai).OrderBy(t => t.ThuTuHienThi), "MaTheLoai", "TenTheLoai", q.TheLoaiId);

            var query = ApplyFilters(
                _context.Sachs.Include(s => s.TheLoai).Include(s => s.NhaXuatBan).Where(s => s.TrangThai), q);
            var r = await PageAsync(query, q.PageNumber, 12);

            ViewData["DangGiu"] = await LayDanhSachDangGiuAsync();
            ViewData["PageNumber"] = r.Page;
            ViewData["TotalPages"] = r.TotalPages;
            ViewData["TotalCount"] = r.Count;
            ViewData["Query"] = q;
            return View(r.Items);
        }

        // Những sách bạn đọc đang mượn / đã gửi yêu cầu (để khóa nút Mượn)
        private async Task<HashSet<int>> LayDanhSachDangGiuAsync()
        {
            if (HttpContext.Session.GetInt32("UserRole") != 3) return new HashSet<int>();
            var userId = HttpContext.Session.GetInt32("UserId");
            var banDoc = await _context.BanDocs.FirstOrDefaultAsync(b => b.MaTaiKhoan == userId);
            if (banDoc == null) return new HashSet<int>();
            var ids = await _context.ChiTietMuons
                .Where(c => c.PhieuMuon.MaBanDoc == banDoc.MaBanDoc && (c.PhieuMuon.TrangThai == 0 || c.PhieuMuon.TrangThai == 1))
                .Select(c => c.MaSach).ToListAsync();
            return ids.ToHashSet();
        }

        // Chi tiết sách (mọi tài khoản đăng nhập)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var sach = await _context.Sachs.Include(s => s.TheLoai).Include(s => s.NhaXuatBan)
                .FirstOrDefaultAsync(s => s.MaSach == id);
            if (sach == null) return NotFound();

            var role = HttpContext.Session.GetInt32("UserRole");
            if (role == 3 && !sach.TrangThai) return NotFound(); // bạn đọc không xem sách đã khóa

            ViewData["DangGiu"] = await LayDanhSachDangGiuAsync();
            if (role == 1 || role == 2)
                ViewData["LuotMuon"] = await _context.ChiTietMuons.CountAsync(c => c.MaSach == id && c.PhieuMuon.TrangThai != 3 && c.PhieuMuon.TrangThai != 0);
            return View(sach);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> ToggleStatus(int id, string? returnTo)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();
            sach.TrangThai = !sach.TrangThai;
            await _context.SaveChangesAsync();
            TempData["Success"] = sach.TrangThai ? $"Đã mở lại sách '{sach.TenSach}'." : $"Đã khóa sách '{sach.TenSach}' (ngưng cho mượn).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Delete(int id)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            // Không xóa sách đã phát sinh lịch sử mượn (giữ toàn vẹn dữ liệu)
            if (await _context.ChiTietMuons.AnyAsync(c => c.MaSach == id))
            {
                TempData["Error"] = $"Sách '{sach.TenSach}' đã phát sinh phiếu mượn nên không thể xóa. Hãy khóa sách (ngưng cho mượn) thay vì xóa.";
                return RedirectToAction(nameof(Index));
            }
            _context.Sachs.Remove(sach);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa sách '{sach.TenSach}'.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdownsAsync(int? maTheLoai = null, int? maNhaXuatBan = null)
        {
            ViewData["MaTheLoai"] = new SelectList(await _context.TheLoais.OrderBy(t => t.ThuTuHienThi).ToListAsync(), "MaTheLoai", "TenTheLoai", maTheLoai);
            ViewData["MaNhaXuatBan"] = new SelectList(await _context.NhaXuatBans.OrderBy(n => n.TenNhaXuatBan).ToListAsync(), "MaNhaXuatBan", "TenNhaXuatBan", maNhaXuatBan);
        }

        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Sach { NamXuatBan = DateTime.Now.Year, SoLuong = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Create(Sach sach)
        {
            if (ModelState.IsValid)
            {
                sach.SoLuongCon = sach.SoLuong; // Khi tạo mới: còn = tổng
                _context.Add(sach);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm sách '{sach.TenSach}'.";
                return RedirectToAction(nameof(Index));
            }
            await LoadDropdownsAsync(sach.MaTheLoai, sach.MaNhaXuatBan);
            return View(sach);
        }

        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            await LoadDropdownsAsync(sach.MaTheLoai, sach.MaNhaXuatBan);
            return View(sach);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Edit(int id, Sach model)
        {
            if (id != model.MaSach) return NotFound();

            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            // SoLuongCon không được sửa trực tiếp: còn lại = tổng - số đang cho mượn
            int dangChoMuon = sach.SoLuong - sach.SoLuongCon;
            if (model.SoLuong < dangChoMuon)
                ModelState.AddModelError(nameof(Sach.SoLuong), $"Tổng số lượng không được nhỏ hơn số cuốn đang cho mượn ({dangChoMuon}).");

            if (ModelState.IsValid)
            {
                sach.TenSach = model.TenSach;
                sach.MaTheLoai = model.MaTheLoai;
                sach.MaNhaXuatBan = model.MaNhaXuatBan;
                sach.NamXuatBan = model.NamXuatBan;
                sach.SoLuong = model.SoLuong;
                sach.SoLuongCon = model.SoLuong - dangChoMuon;
                sach.DonGia = model.DonGia;
                sach.ViTriKeSach = model.ViTriKeSach;
                sach.TrangThai = model.TrangThai;
                sach.GhiChu = model.GhiChu;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật sách '{sach.TenSach}'.";
                return RedirectToAction(nameof(Index));
            }

            model.SoLuongCon = sach.SoLuongCon; // để view hiển thị đúng
            await LoadDropdownsAsync(model.MaTheLoai, model.MaNhaXuatBan);
            return View(model);
        }
    }
}
