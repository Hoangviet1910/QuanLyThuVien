using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Attributes;
using QuanLyThuVien.Models;
using QuanLyThuVien.ViewModels;

namespace QuanLyThuVien.Controllers
{
    public class TaiKhoanController : Controller
    {
        private const int PageSize = 10;
        private readonly ApplicationDbContext _context;

        public TaiKhoanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===================== ĐĂNG NHẬP / ĐĂNG XUẤT =====================

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var username = model.TenDangNhap.Trim();
                var user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.TenDangNhap == username && u.MatKhau == model.MatKhau);

                if (user != null)
                {
                    if (!user.TrangThai)
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                        return View(model);
                    }

                    HttpContext.Session.Clear();
                    HttpContext.Session.SetInt32("UserId", user.MaTaiKhoan);
                    HttpContext.Session.SetString("UserName", user.HoTen);
                    HttpContext.Session.SetInt32("UserRole", user.VaiTro);

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "TaiKhoan");
        }

        // ===================== QUẢN LÝ TÀI KHOẢN (ADMIN) =====================

        [Auth(Roles = "1")]
        public async Task<IActionResult> Index(string? searchString, int? vaiTro, bool? trangThai, int? pageNumber)
        {
            var query = _context.TaiKhoans.Include(t => t.BanDoc).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var key = searchString.Trim();
                query = query.Where(t => t.TenDangNhap.Contains(key) || t.HoTen.Contains(key) || (t.Email != null && t.Email.Contains(key)));
            }
            if (vaiTro.HasValue) query = query.Where(t => t.VaiTro == vaiTro.Value);
            if (trangThai.HasValue) query = query.Where(t => t.TrangThai == trangThai.Value);

            int count = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
            int page = Math.Clamp(pageNumber ?? 1, 1, totalPages);

            var items = await query.OrderBy(t => t.VaiTro).ThenBy(t => t.TenDangNhap)
                .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentVaiTro"] = vaiTro;
            ViewData["CurrentTrangThai"] = trangThai;
            ViewData["PageNumber"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = count;
            ViewData["CurrentUserId"] = HttpContext.Session.GetInt32("UserId");
            return View(items);
        }

        [Auth(Roles = "1")]
        public IActionResult Create() => View(new TaiKhoanFormViewModel { VaiTro = 2 });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1")]
        public async Task<IActionResult> Create(TaiKhoanFormViewModel vm)
        {
            vm.TenDangNhap = vm.TenDangNhap?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(vm.MatKhau))
                ModelState.AddModelError(nameof(vm.MatKhau), "Mật khẩu không được để trống");
            if (vm.VaiTro == 3)
                ModelState.AddModelError(nameof(vm.VaiTro), "Tài khoản Bạn đọc được tạo tự động khi thêm bạn đọc (menu Bạn đọc).");
            if (vm.TenDangNhap.Length > 0 && await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap))
                ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");

            if (!ModelState.IsValid) return View(vm);

            _context.TaiKhoans.Add(new TaiKhoan
            {
                TenDangNhap = vm.TenDangNhap,
                MatKhau = vm.MatKhau!,
                HoTen = vm.HoTen,
                Email = vm.Email,
                VaiTro = vm.VaiTro,
                TrangThai = vm.TrangThai
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo tài khoản '{vm.TenDangNhap}'.";
            return RedirectToAction(nameof(Index));
        }

        [Auth(Roles = "1")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var t = await _context.TaiKhoans.Include(x => x.BanDoc).FirstOrDefaultAsync(x => x.MaTaiKhoan == id);
            if (t == null) return NotFound();
            return View(new TaiKhoanFormViewModel
            {
                MaTaiKhoan = t.MaTaiKhoan, TenDangNhap = t.TenDangNhap, HoTen = t.HoTen, Email = t.Email,
                VaiTro = t.VaiTro, TrangThai = t.TrangThai, LaBanDoc = t.BanDoc != null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1")]
        public async Task<IActionResult> Edit(int id, TaiKhoanFormViewModel vm)
        {
            if (id != vm.MaTaiKhoan) return NotFound();
            var t = await _context.TaiKhoans.Include(x => x.BanDoc).FirstOrDefaultAsync(x => x.MaTaiKhoan == id);
            if (t == null) return NotFound();

            vm.TenDangNhap = vm.TenDangNhap?.Trim() ?? "";
            vm.LaBanDoc = t.BanDoc != null;
            var isSelf = id == HttpContext.Session.GetInt32("UserId");

            if (await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == vm.TenDangNhap && x.MaTaiKhoan != id))
                ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
            if (t.BanDoc != null && vm.VaiTro != 3)
                ModelState.AddModelError(nameof(vm.VaiTro), "Tài khoản đang gắn với hồ sơ bạn đọc, không thể đổi vai trò.");
            if (t.BanDoc == null && vm.VaiTro == 3)
                ModelState.AddModelError(nameof(vm.VaiTro), "Không thể đặt vai trò Bạn đọc cho tài khoản chưa có hồ sơ bạn đọc.");
            if (isSelf && (!vm.TrangThai || vm.VaiTro != t.VaiTro))
                ModelState.AddModelError("", "Bạn không thể tự khóa hoặc tự đổi vai trò của chính mình.");

            if (!ModelState.IsValid) return View(vm);

            t.TenDangNhap = vm.TenDangNhap;
            t.HoTen = vm.HoTen;
            t.Email = vm.Email;
            t.VaiTro = vm.VaiTro;
            t.TrangThai = vm.TrangThai;
            if (!string.IsNullOrWhiteSpace(vm.MatKhau)) t.MatKhau = vm.MatKhau;

            if (t.BanDoc != null) // đồng bộ hồ sơ bạn đọc
            {
                t.BanDoc.HoTen = vm.HoTen;
                t.BanDoc.Email = vm.Email;
                t.BanDoc.TrangThai = vm.TrangThai;
            }

            await _context.SaveChangesAsync();
            if (isSelf) HttpContext.Session.SetString("UserName", t.HoTen);
            TempData["Success"] = $"Đã cập nhật tài khoản '{t.TenDangNhap}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var t = await _context.TaiKhoans.Include(x => x.BanDoc).FirstOrDefaultAsync(x => x.MaTaiKhoan == id);
            if (t == null) return NotFound();
            if (id == HttpContext.Session.GetInt32("UserId"))
            {
                TempData["Error"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }
            t.TrangThai = !t.TrangThai;
            if (t.BanDoc != null) t.BanDoc.TrangThai = t.TrangThai;
            await _context.SaveChangesAsync();
            TempData["Success"] = t.TrangThai ? $"Đã mở khóa '{t.TenDangNhap}'." : $"Đã khóa '{t.TenDangNhap}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Roles = "1")]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.MaTaiKhoan == id);
            if (t == null) return NotFound();

            if (id == HttpContext.Session.GetInt32("UserId"))
                TempData["Error"] = "Bạn không thể xóa tài khoản của chính mình.";
            else if (await _context.BanDocs.AnyAsync(b => b.MaTaiKhoan == id))
                TempData["Error"] = "Tài khoản gắn với hồ sơ bạn đọc — hãy khóa tài khoản thay vì xóa.";
            else if (await _context.PhieuMuons.AnyAsync(p => p.NhanVienXuLy == id))
                TempData["Error"] = "Tài khoản đã xử lý phiếu mượn (có lịch sử) — hãy khóa tài khoản thay vì xóa.";
            else
            {
                _context.TaiKhoans.Remove(t);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa tài khoản '{t.TenDangNhap}'.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
