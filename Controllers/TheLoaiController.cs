using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Controllers
{
    [Auth(Roles = "1")] // Dữ liệu nền: chỉ Admin
    public class TheLoaiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TheLoaiController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.TheLoais.OrderBy(t => t.ThuTuHienThi).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var theLoai = await _context.TheLoais.Include(t => t.Sachs).FirstOrDefaultAsync(t => t.MaTheLoai == id);
            if (theLoai == null) return NotFound();
            return View(theLoai);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var theLoai = await _context.TheLoais.FindAsync(id);
            if (theLoai == null) return NotFound();
            theLoai.TrangThai = !theLoai.TrangThai;
            await _context.SaveChangesAsync();
            TempData["Success"] = theLoai.TrangThai ? $"Đã kích hoạt thể loại '{theLoai.TenTheLoai}'." : $"Đã tạm ẩn thể loại '{theLoai.TenTheLoai}'.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TheLoai theLoai)
        {
            if (ModelState.IsValid)
            {
                if (await _context.TheLoais.AnyAsync(t => t.TenTheLoai == theLoai.TenTheLoai))
                {
                    ModelState.AddModelError("TenTheLoai", "Tên thể loại đã tồn tại");
                    return View(theLoai);
                }
                
                _context.Add(theLoai);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm thể loại.";
                return RedirectToAction(nameof(Index));
            }
            return View(theLoai);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var theLoai = await _context.TheLoais.FindAsync(id);
            if (theLoai == null) return NotFound();
            
            return View(theLoai);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TheLoai theLoai)
        {
            if (id != theLoai.MaTheLoai) return NotFound();

            if (ModelState.IsValid)
            {
                if (await _context.TheLoais.AnyAsync(t => t.TenTheLoai == theLoai.TenTheLoai && t.MaTheLoai != id))
                {
                    ModelState.AddModelError("TenTheLoai", "Tên thể loại đã tồn tại");
                    return View(theLoai);
                }

                try
                {
                    _context.Update(theLoai);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TheLoaiExists(theLoai.MaTheLoai)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(theLoai);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var theLoai = await _context.TheLoais.Include(t => t.Sachs).FirstOrDefaultAsync(t => t.MaTheLoai == id);
            if (theLoai != null)
            {
                if (theLoai.Sachs.Any())
                {
                    TempData["Error"] = "Không thể xóa thể loại đã có sách!";
                    return RedirectToAction(nameof(Index));
                }
                _context.TheLoais.Remove(theLoai);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thể loại.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TheLoaiExists(int id)
        {
            return _context.TheLoais.Any(e => e.MaTheLoai == id);
        }
    }
}
