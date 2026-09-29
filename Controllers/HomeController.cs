using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;
using QuanLyThuVien.ViewModels;
using System.Diagnostics;

namespace QuanLyThuVien.Controllers
{
    [Auth]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetInt32("UserRole");
            var vm = new DashboardViewModel();
            var today = DateTime.Today;

            if (role == 1 || role == 2)
            {
                vm.TongSach = await _context.Sachs.SumAsync(s => (int?)s.SoLuong) ?? 0;
                vm.SachKhaDung = await _context.Sachs.SumAsync(s => (int?)s.SoLuongCon) ?? 0;
                vm.TongBanDoc = await _context.BanDocs.CountAsync(b => b.TrangThai);
                vm.PhieuChoDuyet = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 0);
                vm.PhieuDangMuon = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 1);
                vm.PhieuQuaHan = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 1 && p.HanTra < today);
                vm.TongTheLoai = await _context.TheLoais.CountAsync();
                vm.TongPhieu = await _context.PhieuMuons.CountAsync();
                vm.PhieuHoanThanh = await _context.PhieuMuons.CountAsync(p => p.TrangThai == 2);
                vm.TongTienPhat = await _context.PhieuMuons.SumAsync(p => (decimal?)p.TienPhat) ?? 0m;

                vm.ThongKeTheLoai = await _context.TheLoais
                    .OrderBy(t => t.ThuTuHienThi)
                    .Select(t => new TheLoaiThongKe
                    {
                        TenTheLoai = t.TenTheLoai,
                        SoDauSach = t.Sachs.Count(),
                        SoLuong = t.Sachs.Sum(s => (int?)s.SoLuong) ?? 0
                    })
                    .ToListAsync();

                vm.PhieuMoiNhat = await _context.PhieuMuons
                    .Include(p => p.BanDoc)
                    .OrderByDescending(p => p.MaPhieuMuon)
                    .Take(5)
                    .ToListAsync();
            }
            else
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var banDoc = await _context.BanDocs.FirstOrDefaultAsync(b => b.MaTaiKhoan == userId);
                if (banDoc == null)
                {
                    vm.CoHoSoBanDoc = false;
                }
                else
                {
                    vm.BanDoc = banDoc;
                    var phieuCuaToi = _context.PhieuMuons.Where(p => p.MaBanDoc == banDoc.MaBanDoc);
                    vm.PhieuDangMuon = await phieuCuaToi.CountAsync(p => p.TrangThai == 1);
                    vm.PhieuQuaHan = await phieuCuaToi.CountAsync(p => p.TrangThai == 1 && p.HanTra < today);
                    vm.PhieuChoDuyetCuaToi = await phieuCuaToi.CountAsync(p => p.TrangThai == 0);

                    vm.SachDangMuonCuaToi = await _context.ChiTietMuons
                        .Include(c => c.Sach)
                        .Include(c => c.PhieuMuon)
                        .Where(c => c.PhieuMuon.MaBanDoc == banDoc.MaBanDoc && c.PhieuMuon.TrangThai == 1)
                        .OrderBy(c => c.PhieuMuon.HanTra)
                        .ToListAsync();
                }
            }

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
