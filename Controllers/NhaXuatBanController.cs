using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Controllers
{
    [Auth(Roles = "1")] // Dữ liệu nền: chỉ Admin
    public class NhaXuatBanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NhaXuatBanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.NhaXuatBans.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhaXuatBan nhaXuatBan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(nhaXuatBan);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm nhà xuất bản '{nhaXuatBan.TenNhaXuatBan}'.";
                return RedirectToAction(nameof(Index));
            }
            return View(nhaXuatBan);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var nxb = await _context.NhaXuatBans.FindAsync(id);
            if (nxb == null) return NotFound();
            return View(nxb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhaXuatBan nhaXuatBan)
        {
            if (id != nhaXuatBan.MaNhaXuatBan) return NotFound();
            if (!await _context.NhaXuatBans.AnyAsync(n => n.MaNhaXuatBan == id)) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(nhaXuatBan);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật nhà xuất bản '{nhaXuatBan.TenNhaXuatBan}'.";
                return RedirectToAction(nameof(Index));
            }
            return View(nhaXuatBan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var nxb = await _context.NhaXuatBans.Include(n => n.Sachs).FirstOrDefaultAsync(n => n.MaNhaXuatBan == id);
            if (nxb != null)
            {
                if (nxb.Sachs.Any())
                {
                    TempData["Error"] = "Không thể xóa nhà xuất bản đã có sách!";
                    return RedirectToAction(nameof(Index));
                }
                _context.NhaXuatBans.Remove(nxb);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa nhà xuất bản.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
