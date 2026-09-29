using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;
using QuanLyThuVien.ViewModels;

namespace QuanLyThuVien.Controllers
{
    /// <summary>Thống kê (LINQ), xử lý quá hạn và báo cáo — dành cho Admin và Thủ thư.</summary>
    [Auth(Roles = "1,2")]
    public class ThongKeController : Controller
    {
        private const decimal PhiPhatMoiNgay = 5000m;
        private readonly ApplicationDbContext _context;

        public ThongKeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var vm = new ThongKeViewModel();

            // ---- Tổng quan: Count() ----
            vm.TongTheLoai = await _context.TheLoais.CountAsync();
            vm.TongDauSach = await _context.Sachs.CountAsync();
            vm.TongBanDoc = await _context.BanDocs.CountAsync();
            vm.TongPhieu = await _context.PhieuMuons.CountAsync();
            vm.ChoXuLy = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 0);
            vm.DangXuLy = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 1);
            vm.HoanThanh = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 2);
            vm.HuyTuChoi = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 3);

            // ---- Số phiếu quá hạn: đang mượn mà HanTra < hôm nay ----
            vm.QuaHanDangMuon = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 1 && p.HanTra < today);

            // ---- Tỷ lệ trả đúng hạn: phiếu hoàn thành có NgayTra trong ngày HanTra hoặc sớm hơn ----
            vm.DaTra = vm.HoanThanh;
            vm.TraDungHan = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 2 && p.NgayTra != null && p.NgayTra < p.HanTra.AddDays(1));
            vm.TraTre = vm.DaTra - vm.TraDungHan;

            // ---- Tổng tiền phạt: Sum() ----
            vm.TongTienPhat = await _context.PhieuMuons.Where(p => p.TrangThai == 2).SumAsync(p => (decimal?)p.TienPhat) ?? 0m;

            // ---- Số sách theo thể loại: GroupBy/Sum ----
            vm.SachTheoTheLoai = await _context.TheLoais
                .OrderBy(t => t.ThuTuHienThi)
                .Select(t => new TheLoaiThongKe
                {
                    TenTheLoai = t.TenTheLoai,
                    SoDauSach = t.Sachs.Count(),
                    SoLuong = t.Sachs.Sum(s => (int?)s.SoLuong) ?? 0
                }).ToListAsync();

            // ---- Số lượt mượn theo tháng (12 tháng gần nhất; chỉ tính phiếu đã được duyệt: trạng thái 1, 2) ----
            var from = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
            var phieuTheoThang = await _context.PhieuMuons
                .Where(p => (p.TrangThai == 1 || p.TrangThai == 2) && p.NgayMuon >= from)
                .GroupBy(p => new { p.NgayMuon.Year, p.NgayMuon.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, SoPhieu = g.Count() })
                .ToListAsync();
            var cuonTheoThang = await _context.ChiTietMuons
                .Where(c => (c.PhieuMuon.TrangThai == 1 || c.PhieuMuon.TrangThai == 2) && c.PhieuMuon.NgayMuon >= from)
                .GroupBy(c => new { c.PhieuMuon.NgayMuon.Year, c.PhieuMuon.NgayMuon.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, SoCuon = g.Sum(c => c.SoLuong) })
                .ToListAsync();
            for (int i = 0; i < 12; i++)
            {
                var m = from.AddMonths(i);
                vm.LuotMuonTheoThang.Add(new ThangThongKe
                {
                    Thang = $"{m.Month:00}/{m.Year}",
                    SoPhieu = phieuTheoThang.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.SoPhieu ?? 0,
                    SoCuon = cuonTheoThang.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.SoCuon ?? 0
                });
            }

            // ---- Sách được mượn nhiều nhất: GroupBy + OrderByDescending ----
            vm.TopSach = await _context.ChiTietMuons
                .Where(c => c.PhieuMuon.TrangThai == 1 || c.PhieuMuon.TrangThai == 2)
                .GroupBy(c => new { c.MaSach, c.Sach.TenSach })
                .Select(g => new TopItem { Ten = g.Key.TenSach, SoLuot = g.Sum(c => c.SoLuong) })
                .OrderByDescending(x => x.SoLuot).ThenBy(x => x.Ten)
                .Take(5).ToListAsync();

            // ---- Bạn đọc mượn nhiều nhất ----
            vm.TopBanDoc = await _context.PhieuMuons
                .Where(p => p.TrangThai == 1 || p.TrangThai == 2)
                .GroupBy(p => new { p.MaBanDoc, p.BanDoc.HoTen })
                .Select(g => new TopItem { Ten = g.Key.HoTen, SoLuot = g.Count() })
                .OrderByDescending(x => x.SoLuot).ThenBy(x => x.Ten)
                .Take(5).ToListAsync();

            return View(vm);
        }

        // ---- Xử lý quá hạn: danh sách phiếu đang mượn đã quá hạn ----
        public async Task<IActionResult> QuaHan()
        {
            var today = DateTime.Today;
            var list = await _context.PhieuMuons
                .Include(p => p.BanDoc)
                .Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                .Where(p => p.TrangThai == 1 && p.HanTra < today)
                .OrderBy(p => p.HanTra)
                .ToListAsync();
            ViewData["PhiPhat"] = PhiPhatMoiNgay;
            return View(list);
        }

        // ---- Báo cáo: xuất CSV (mở được bằng Excel) ----
        [HttpGet]
        public async Task<IActionResult> XuatBaoCao(string loai = "phieu", DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            var today = DateTime.Today;
            var sb = new StringBuilder();
            static string Q(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
            static string TT(int t) => t switch { 0 => "Chờ xử lý", 1 => "Đang mượn", 2 => "Hoàn thành", _ => "Hủy/Từ chối" };

            if (loai == "quahan")
            {
                sb.AppendLine("Mã phiếu,Bạn đọc,SĐT,Sách,Ngày mượn,Hạn trả,Số ngày quá hạn,Tiền phạt tạm tính");
                var list = await _context.PhieuMuons.Include(p => p.BanDoc).Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach)
                    .Where(p => p.TrangThai == 1 && p.HanTra < today).OrderBy(p => p.HanTra).ToListAsync();
                foreach (var p in list)
                {
                    int ngay = (today - p.HanTra.Date).Days;
                    decimal phat = ngay * PhiPhatMoiNgay * p.ChiTietMuons.Sum(c => c.SoLuong);
                    sb.AppendLine(string.Join(",", p.MaPhieuMuon, Q(p.BanDoc.HoTen), Q(p.BanDoc.SoDienThoai),
                        Q(string.Join("; ", p.ChiTietMuons.Select(c => c.Sach.TenSach))),
                        p.NgayMuon.ToString("dd/MM/yyyy"), p.HanTra.ToString("dd/MM/yyyy"), ngay, phat.ToString("0")));
                }
            }
            else
            {
                sb.AppendLine("Mã phiếu,Bạn đọc,Sách,Ngày mượn,Hạn trả,Ngày trả,Trạng thái,Tiền phạt");
                var q = _context.PhieuMuons.Include(p => p.BanDoc).Include(p => p.ChiTietMuons).ThenInclude(c => c.Sach).AsQueryable();
                if (tuNgay.HasValue) q = q.Where(p => p.NgayMuon >= tuNgay.Value.Date);
                if (denNgay.HasValue) q = q.Where(p => p.NgayMuon < denNgay.Value.Date.AddDays(1));
                foreach (var p in await q.OrderBy(p => p.NgayMuon).ToListAsync())
                {
                    sb.AppendLine(string.Join(",", p.MaPhieuMuon, Q(p.BanDoc.HoTen),
                        Q(string.Join("; ", p.ChiTietMuons.Select(c => c.Sach.TenSach))),
                        p.NgayMuon.ToString("dd/MM/yyyy"), p.HanTra.ToString("dd/MM/yyyy"),
                        p.NgayTra?.ToString("dd/MM/yyyy") ?? "", Q(TT(p.TrangThai)), p.TienPhat.ToString("0")));
                }
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(); // BOM để Excel đọc đúng tiếng Việt
            return File(bytes, "text/csv", $"bao-cao-{loai}-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        }
    }
}
