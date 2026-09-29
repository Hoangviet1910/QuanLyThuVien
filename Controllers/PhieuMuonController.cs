using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;
using QuanLyThuVien.ViewModels;

namespace QuanLyThuVien.Controllers
{
    [Auth]
    public class PhieuMuonController : Controller
    {
        private const int SoNgayMuonMacDinh = 14;      // Hạn trả mặc định
        private const decimal PhiPhatMoiNgay = 5000m;  // 5.000 VNĐ / ngày / cuốn
        private const int SoSachToiDa = 5;             // Tối đa số cuốn 1 bạn đọc được giữ (chờ duyệt + đang mượn)
        private const int PageSize = 10;

        private readonly ApplicationDbContext _context;

        public PhieuMuonController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
        private int? CurrentRole => HttpContext.Session.GetInt32("UserRole");

        private Task<BanDoc?> GetCurrentBanDocAsync()
        {
            var userId = CurrentUserId;
            return _context.BanDocs.FirstOrDefaultAsync(b => b.MaTaiKhoan == userId);
        }

        private static string? KiemTraThe(BanDoc? banDoc, bool laChinhMinh = false)
        {
            if (banDoc == null) return "Không tìm thấy bạn đọc.";
            var ten = laChinhMinh ? "của bạn" : $"của bạn đọc '{banDoc.HoTen}'";
            if (!banDoc.TrangThai) return $"Thẻ {ten} đang bị khóa.";
            if (banDoc.HanThe.Date < DateTime.Today) return $"Thẻ {ten} đã hết hạn ngày {banDoc.HanThe:dd/MM/yyyy}.";
            return null;
        }

        private async Task<int> DemSachDangGiuAsync(int maBanDoc)
        {
            var tong = await _context.ChiTietMuons
                .Where(c => c.PhieuMuon.MaBanDoc == maBanDoc && (c.PhieuMuon.TrangThai == 0 || c.PhieuMuon.TrangThai == 1))
                .SumAsync(c => (int?)c.SoLuong);
            return tong ?? 0;
        }

        // ===================== THỦ THƯ / ADMIN =====================

        // trangThai: 0,1,2,3 hoặc 4 (= quá hạn)
        // Tìm theo bạn đọc / mã phiếu / tên sách; lọc theo thể loại, trạng thái, khoảng ngày; sắp xếp theo ngày hoặc tên
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Index(string? searchString, int? trangThai, int? theLoaiId,
            DateTime? tuNgay, DateTime? denNgay, string? sortOrder, int? pageNumber)
        {
            var today = DateTime.Today;
            var query = _context.PhieuMuons
                .Include(p => p.BanDoc)
                .Include(p => p.NguoiXuLy)
                .Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var key = searchString.Trim().TrimStart('#');
                if (int.TryParse(key, out var maPhieu))
                    query = query.Where(p => p.MaPhieuMuon == maPhieu || p.BanDoc.HoTen.Contains(key)
                        || p.ChiTietMuons.Any(c => c.Sach.TenSach.Contains(key)));
                else
                    query = query.Where(p => p.BanDoc.HoTen.Contains(key)
                        || p.ChiTietMuons.Any(c => c.Sach.TenSach.Contains(key)));
            }

            if (theLoaiId.HasValue)
                query = query.Where(p => p.ChiTietMuons.Any(c => c.Sach.MaTheLoai == theLoaiId.Value));
            if (tuNgay.HasValue)
                query = query.Where(p => p.NgayMuon >= tuNgay.Value.Date);
            if (denNgay.HasValue)
                query = query.Where(p => p.NgayMuon < denNgay.Value.Date.AddDays(1));

            if (trangThai == 4) query = query.Where(p => p.TrangThai == 1 && p.HanTra < today);
            else if (trangThai.HasValue) query = query.Where(p => p.TrangThai == trangThai.Value);

            query = sortOrder switch
            {
                "date" => query.OrderBy(p => p.NgayMuon).ThenBy(p => p.MaPhieuMuon),
                "name" => query.OrderBy(p => p.BanDoc.HoTen).ThenByDescending(p => p.MaPhieuMuon),
                "name_desc" => query.OrderByDescending(p => p.BanDoc.HoTen).ThenByDescending(p => p.MaPhieuMuon),
                _ => query.OrderByDescending(p => p.NgayMuon).ThenByDescending(p => p.MaPhieuMuon)
            };

            var count = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
            var page = Math.Clamp(pageNumber ?? 1, 1, totalPages);

            var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            var counts = await _context.PhieuMuons
                .GroupBy(p => p.TrangThai)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
            counts[4] = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 1 && p.HanTra < today);

            ViewData["TheLoaiId"] = new SelectList(_context.TheLoais.OrderBy(t => t.ThuTuHienThi), "MaTheLoai", "TenTheLoai", theLoaiId);
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentTrangThai"] = trangThai;
            ViewData["CurrentTheLoai"] = theLoaiId;
            ViewData["TuNgay"] = tuNgay?.ToString("yyyy-MM-dd");
            ViewData["DenNgay"] = denNgay?.ToString("yyyy-MM-dd");
            ViewData["CurrentSort"] = sortOrder;
            ViewData["PageNumber"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = count;
            ViewData["Counts"] = counts;
            return View(items);
        }

        // Xem chi tiết: nhân viên xem tất cả; bạn đọc chỉ xem phiếu của chính mình
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var phieu = await _context.PhieuMuons
                .Include(p => p.BanDoc)
                .Include(p => p.NguoiXuLy)
                .Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == id);
            if (phieu == null) return NotFound();

            if (CurrentRole == 3)
            {
                var banDoc = await GetCurrentBanDocAsync();
                if (banDoc == null || phieu.MaBanDoc != banDoc.MaBanDoc) return NotFound();
            }
            return View(phieu);
        }

        private async Task LoadCreateDataAsync(int? maBanDoc)
        {
            var today = DateTime.Today;
            var raw = await _context.BanDocs
                .Where(b => b.TrangThai && b.HanThe >= today)
                .OrderBy(b => b.HoTen)
                .Select(b => new { b.MaBanDoc, b.HoTen, b.HanThe })
                .ToListAsync();
            var banDocs = raw.Select(b => new { b.MaBanDoc, Text = $"{b.HoTen} (Thẻ #{b.MaBanDoc} - HSD {b.HanThe:dd/MM/yyyy})" }).ToList();
            ViewData["MaBanDoc"] = new SelectList(banDocs, "MaBanDoc", "Text", maBanDoc);
            ViewData["Sachs"] = await _context.Sachs
                .Include(s => s.TheLoai)
                .Where(s => s.TrangThai && s.SoLuongCon > 0)
                .OrderBy(s => s.TenSach)
                .ToListAsync();
        }

        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Create(int? maBanDoc)
        {
            await LoadCreateDataAsync(maBanDoc);
            return View(new PhieuMuonCreateViewModel { MaBanDoc = maBanDoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Create(PhieuMuonCreateViewModel vm)
        {
            var selected = vm.SelectedBooks.Distinct().ToList();
            if (selected.Count == 0)
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một cuốn sách.");

            if (vm.HanTra.Date < DateTime.Today)
                ModelState.AddModelError(nameof(vm.HanTra), "Hạn trả không được nhỏ hơn ngày hôm nay.");

            BanDoc? banDoc = null;
            var sachs = new List<Sach>();

            if (vm.MaBanDoc.HasValue)
            {
                banDoc = await _context.BanDocs.FindAsync(vm.MaBanDoc.Value);
                var loiThe = KiemTraThe(banDoc);
                if (loiThe != null) ModelState.AddModelError(nameof(vm.MaBanDoc), loiThe);
            }

            if (selected.Count > 0)
            {
                sachs = await _context.Sachs.Where(s => selected.Contains(s.MaSach)).ToListAsync();
                foreach (var id in selected)
                {
                    var s = sachs.FirstOrDefault(x => x.MaSach == id);
                    if (s == null) ModelState.AddModelError("", $"Sách mã {id} không tồn tại.");
                    else if (!s.TrangThai || s.SoLuongCon < 1) ModelState.AddModelError("", $"Sách '{s.TenSach}' hiện đã hết hoặc ngưng cho mượn.");
                }
            }

            if (banDoc != null && selected.Count > 0 && ModelState.IsValid)
            {
                // Không tạo phiếu trùng: bạn đọc đang mượn / đang chờ duyệt cùng cuốn sách
                var trung = await _context.ChiTietMuons
                    .Where(c => selected.Contains(c.MaSach) && c.PhieuMuon.MaBanDoc == banDoc.MaBanDoc
                                && (c.PhieuMuon.TrangThai == 0 || c.PhieuMuon.TrangThai == 1))
                    .Select(c => c.Sach.TenSach).Distinct().ToListAsync();
                foreach (var ten in trung)
                    ModelState.AddModelError("", $"Bạn đọc đang mượn hoặc đang chờ duyệt cuốn '{ten}' (không tạo phiếu trùng).");
            }

            if (banDoc != null && selected.Count > 0 && ModelState.IsValid)
            {
                var dangGiu = await DemSachDangGiuAsync(banDoc.MaBanDoc);
                if (dangGiu + selected.Count > SoSachToiDa)
                    ModelState.AddModelError("", $"Bạn đọc đang giữ {dangGiu} cuốn; mỗi bạn đọc chỉ được mượn tối đa {SoSachToiDa} cuốn cùng lúc.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateDataAsync(vm.MaBanDoc);
                return View(vm);
            }

            var phieu = new PhieuMuon
            {
                MaBanDoc = banDoc!.MaBanDoc,
                NgayMuon = DateTime.Now,
                HanTra = vm.HanTra.Date,
                TrangThai = 1,
                GhiChu = vm.GhiChu,
                NhanVienXuLy = CurrentUserId
            };
            foreach (var sach in sachs)
            {
                phieu.ChiTietMuons.Add(new ChiTietMuon { MaSach = sach.MaSach, SoLuong = 1, TinhTrangMuon = "Bình thường" });
                sach.SoLuongCon -= 1; // trừ tồn kho
            }

            _context.PhieuMuons.Add(phieu);
            await _context.SaveChangesAsync(); // một lần SaveChanges => cùng một transaction

            TempData["Success"] = $"Đã tạo phiếu mượn #{phieu.MaPhieuMuon} cho {banDoc.HoTen} ({sachs.Count} cuốn, hạn trả {phieu.HanTra:dd/MM/yyyy}).";
            return RedirectToAction(nameof(Details), new { id = phieu.MaPhieuMuon });
        }

        // Duyệt yêu cầu mượn (0 -> 1), trừ tồn kho tại thời điểm duyệt
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> Duyet(int id)
        {
            var phieu = await _context.PhieuMuons
                .Include(p => p.BanDoc)
                .Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == id);
            if (phieu == null) return NotFound();

            if (phieu.TrangThai != 0)
            {
                TempData["Warning"] = $"Phiếu #{id} không ở trạng thái chờ duyệt.";
                return RedirectToAction(nameof(Index));
            }

            var loiThe = KiemTraThe(phieu.BanDoc);
            if (loiThe != null)
            {
                TempData["Error"] = $"Không thể duyệt phiếu #{id}: {loiThe}";
                return RedirectToAction(nameof(Index));
            }

            foreach (var ct in phieu.ChiTietMuons)
            {
                if (!ct.Sach.TrangThai || ct.Sach.SoLuongCon < ct.SoLuong)
                {
                    TempData["Error"] = $"Không thể duyệt phiếu #{id}: sách '{ct.Sach.TenSach}' không đủ số lượng.";
                    return RedirectToAction(nameof(Index));
                }
            }

            foreach (var ct in phieu.ChiTietMuons) ct.Sach.SoLuongCon -= ct.SoLuong;

            phieu.TrangThai = 1;
            phieu.NgayMuon = DateTime.Now;
            phieu.HanTra = DateTime.Today.AddDays(SoNgayMuonMacDinh);
            phieu.NhanVienXuLy = CurrentUserId;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã duyệt phiếu #{id}. Hạn trả: {phieu.HanTra:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Index));
        }

        // Từ chối yêu cầu mượn (0 -> 3)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> TuChoi(int id, string? lyDo)
        {
            var phieu = await _context.PhieuMuons.FindAsync(id);
            if (phieu == null) return NotFound();

            if (phieu.TrangThai != 0)
            {
                TempData["Warning"] = $"Phiếu #{id} không ở trạng thái chờ duyệt.";
                return RedirectToAction(nameof(Index));
            }

            phieu.TrangThai = 3;
            phieu.NhanVienXuLy = CurrentUserId;
            var ghiChu = string.IsNullOrWhiteSpace(lyDo) ? "Thủ thư từ chối yêu cầu." : "Từ chối: " + lyDo.Trim();
            phieu.GhiChu = string.IsNullOrWhiteSpace(phieu.GhiChu) ? ghiChu : phieu.GhiChu + " | " + ghiChu;
            await _context.SaveChangesAsync();

            TempData["Info"] = $"Đã từ chối phiếu #{id}.";
            return RedirectToAction(nameof(Index));
        }

        private static readonly string[] TinhTrangTraHopLe = { "Bình thường", "Hư hỏng nhẹ", "Hư hỏng nặng", "Mất sách" };

        // Trả sách: 1 -> 2, cộng lại tồn kho, tính phạt quá hạn (5.000đ/ngày/cuốn); mất sách: bồi thường theo đơn giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1,2")]
        public async Task<IActionResult> ReturnBooks(int id, bool fromDetails = false, Dictionary<int, string>? tinhTrangTra = null)
        {
            var phieu = await _context.PhieuMuons
                .Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                .FirstOrDefaultAsync(p => p.MaPhieuMuon == id);
            if (phieu == null) return NotFound();

            if (phieu.TrangThai != 1)
            {
                TempData["Warning"] = $"Phiếu #{id} không ở trạng thái đang mượn.";
                return RedirectToAction(nameof(Index));
            }

            var ngayTra = DateTime.Now; // thời điểm do hệ thống xác định
            int soNgayQuaHan = Math.Max(0, (ngayTra.Date - phieu.HanTra.Date).Days);
            decimal tongPhat = 0;
            int soMat = 0;

            foreach (var ct in phieu.ChiTietMuons)
            {
                var tt = "Bình thường";
                if (tinhTrangTra != null && tinhTrangTra.TryGetValue(ct.MaChiTiet, out var v) && TinhTrangTraHopLe.Contains(v)) tt = v;

                ct.TinhTrangTra = tt;
                ct.SoNgayQuaHan = soNgayQuaHan;
                ct.TienPhat = soNgayQuaHan * PhiPhatMoiNgay * ct.SoLuong;

                if (tt == "Mất sách")
                {
                    ct.TienPhat += ct.Sach.DonGia * ct.SoLuong; // bồi thường
                    ct.Sach.SoLuong -= ct.SoLuong;              // sách mất: không nhập lại kho, giảm tổng số
                    soMat++;
                }
                else
                {
                    ct.Sach.SoLuongCon += ct.SoLuong;           // cộng lại tồn kho
                }
                tongPhat += ct.TienPhat;
            }

            phieu.TrangThai = 2;
            phieu.NgayTra = ngayTra;
            phieu.TienPhat = tongPhat;
            phieu.NhanVienXuLy = CurrentUserId;
            await _context.SaveChangesAsync();

            if (tongPhat > 0)
                TempData["Warning"] = $"Đã nhận trả phiếu #{id}. " + (soNgayQuaHan > 0 ? $"Quá hạn {soNgayQuaHan} ngày. " : "") + (soMat > 0 ? $"{soMat} cuốn báo mất. " : "") + $"Tiền phạt: {tongPhat:N0} VNĐ.";
            else
                TempData["Success"] = $"Đã nhận trả sách cho phiếu #{id}.";

            return fromDetails
                ? RedirectToAction(nameof(Details), new { id })
                : RedirectToAction(nameof(Index));
        }

        // ===================== BẠN ĐỌC =====================

        [Auth(Roles = "3")]
        public async Task<IActionResult> LichSu(int? trangThai, int? pageNumber)
        {
            var banDoc = await GetCurrentBanDocAsync();
            if (banDoc == null)
            {
                TempData["Error"] = "Tài khoản của bạn chưa được liên kết với hồ sơ bạn đọc. Vui lòng liên hệ thủ thư.";
                return RedirectToAction("Index", "Home");
            }

            var today = DateTime.Today;
            var query = _context.PhieuMuons
                .Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                .Where(p => p.MaBanDoc == banDoc.MaBanDoc); // chỉ dữ liệu của chính mình

            if (trangThai == 4) query = query.Where(p => p.TrangThai == 1 && p.HanTra < today);
            else if (trangThai.HasValue) query = query.Where(p => p.TrangThai == trangThai.Value);

            var count = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
            var page = Math.Clamp(pageNumber ?? 1, 1, totalPages);

            var items = await query
                .OrderByDescending(p => p.MaPhieuMuon)
                .Skip((page - 1) * PageSize).Take(PageSize)
                .ToListAsync();

            ViewData["BanDoc"] = banDoc;
            ViewData["CurrentTrangThai"] = trangThai;
            ViewData["PageNumber"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TongPhat"] = await _context.PhieuMuons
                .Where(p => p.MaBanDoc == banDoc.MaBanDoc).SumAsync(p => (decimal?)p.TienPhat) ?? 0m;
            return View(items);
        }

        // Bạn đọc gửi yêu cầu mượn 1 cuốn sách => phiếu "Chờ duyệt"
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "3")]
        public async Task<IActionResult> DangKyMuon(int maSach)
        {
            var banDoc = await GetCurrentBanDocAsync();
            var loiThe = banDoc == null ? "Tài khoản chưa có hồ sơ bạn đọc." : KiemTraThe(banDoc, laChinhMinh: true);
            if (loiThe != null)
            {
                TempData["Error"] = loiThe;
                return RedirectToAction("TraCuu", "Sach");
            }

            var sach = await _context.Sachs.FindAsync(maSach);
            if (sach == null || !sach.TrangThai)
            {
                TempData["Error"] = "Sách không tồn tại hoặc đã ngưng cho mượn.";
                return RedirectToAction("TraCuu", "Sach");
            }
            if (sach.SoLuongCon < 1)
            {
                TempData["Warning"] = $"Sách '{sach.TenSach}' hiện đã hết.";
                return RedirectToAction("TraCuu", "Sach");
            }

            var daCo = await _context.ChiTietMuons.AnyAsync(c => c.MaSach == maSach
                && c.PhieuMuon.MaBanDoc == banDoc!.MaBanDoc
                && (c.PhieuMuon.TrangThai == 0 || c.PhieuMuon.TrangThai == 1));
            if (daCo)
            {
                TempData["Warning"] = $"Bạn đang mượn hoặc đã gửi yêu cầu mượn '{sach.TenSach}'.";
                return RedirectToAction("TraCuu", "Sach");
            }

            var dangGiu = await DemSachDangGiuAsync(banDoc!.MaBanDoc);
            if (dangGiu >= SoSachToiDa)
            {
                TempData["Error"] = $"Bạn đã đạt giới hạn {SoSachToiDa} cuốn (đang mượn + chờ duyệt).";
                return RedirectToAction("TraCuu", "Sach");
            }

            var phieu = new PhieuMuon
            {
                MaBanDoc = banDoc.MaBanDoc,
                NgayMuon = DateTime.Now,
                HanTra = DateTime.Today.AddDays(SoNgayMuonMacDinh),
                TrangThai = 0,
                GhiChu = "Bạn đọc gửi yêu cầu mượn trực tuyến."
            };
            phieu.ChiTietMuons.Add(new ChiTietMuon { MaSach = maSach, SoLuong = 1, TinhTrangMuon = "Bình thường" });
            _context.PhieuMuons.Add(phieu);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gửi yêu cầu mượn '{sach.TenSach}'. Vui lòng chờ thủ thư duyệt.";
            return RedirectToAction(nameof(LichSu));
        }

        // Bạn đọc tự hủy yêu cầu đang chờ duyệt (0 -> 3)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "3")]
        public async Task<IActionResult> HuyYeuCau(int id)
        {
            var banDoc = await GetCurrentBanDocAsync();
            var phieu = await _context.PhieuMuons.FirstOrDefaultAsync(p => p.MaPhieuMuon == id && p.MaBanDoc == (banDoc == null ? -1 : banDoc.MaBanDoc));
            if (phieu == null) return NotFound();

            if (phieu.TrangThai != 0)
            {
                TempData["Warning"] = "Chỉ có thể hủy yêu cầu đang chờ duyệt.";
                return RedirectToAction(nameof(LichSu));
            }

            phieu.TrangThai = 3;
            phieu.GhiChu = (phieu.GhiChu ?? "") + " | Bạn đọc tự hủy yêu cầu.";
            await _context.SaveChangesAsync();

            TempData["Info"] = $"Đã hủy yêu cầu #{id}.";
            return RedirectToAction(nameof(LichSu));
        }
    }
}
