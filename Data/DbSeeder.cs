using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Data
{
    /// <summary>
    /// Dữ liệu mẫu (chạy 1 lần khi bảng Sach còn trống):
    /// 5 thể loại, 6 NXB, 15 sách, 2 admin, 3 thủ thư, 30 bạn đọc, 45 phiếu mượn nhiều trạng thái.
    /// Mật khẩu tất cả tài khoản mẫu: 123456
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            if (db.Sachs.Any()) return;

            using var tx = db.Database.BeginTransaction();
            var today = DateTime.Today;
            var rnd = new Random(2026);

            // ---- Thể loại: giữ 5 thể loại đã có (seed từ migration) ----
            if (!db.TheLoais.Any())
            {
                db.TheLoais.AddRange(
                    new TheLoai { TenTheLoai = "Tiểu thuyết", ThuTuHienThi = 1, TrangThai = true },
                    new TheLoai { TenTheLoai = "Khoa học", ThuTuHienThi = 2, TrangThai = true },
                    new TheLoai { TenTheLoai = "Văn học", ThuTuHienThi = 3, TrangThai = true },
                    new TheLoai { TenTheLoai = "Lịch sử", ThuTuHienThi = 4, TrangThai = true },
                    new TheLoai { TenTheLoai = "Công nghệ", ThuTuHienThi = 5, TrangThai = true });
                db.SaveChanges();
            }
            var theLoais = db.TheLoais.OrderBy(t => t.ThuTuHienThi).ToList();

            // ---- Nhà xuất bản ----
            var nxbs = new List<NhaXuatBan>
            {
                new() { TenNhaXuatBan = "NXB Kim Đồng", DiaChi = "55 Quang Trung, Hà Nội", SoDienThoai = "0243943xxxx", Email = "kimdong@nxb.vn" },
                new() { TenNhaXuatBan = "NXB Trẻ", DiaChi = "161B Lý Chính Thắng, TP.HCM", SoDienThoai = "0283930xxxx", Email = "nxbtre@nxb.vn" },
                new() { TenNhaXuatBan = "NXB Giáo Dục Việt Nam", DiaChi = "81 Trần Hưng Đạo, Hà Nội", SoDienThoai = "0243822xxxx", Email = "gd@nxb.vn" },
                new() { TenNhaXuatBan = "NXB Khoa Học Kỹ Thuật", DiaChi = "70 Trần Hưng Đạo, Hà Nội", SoDienThoai = "0243942xxxx", Email = "khkt@nxb.vn" },
                new() { TenNhaXuatBan = "NXB Văn Học", DiaChi = "18 Nguyễn Trường Tộ, Hà Nội", SoDienThoai = "0243716xxxx", Email = "vanhoc@nxb.vn" },
                new() { TenNhaXuatBan = "NXB Lao Động", DiaChi = "175 Giảng Võ, Hà Nội", SoDienThoai = "0243851xxxx", Email = "laodong@nxb.vn" },
            };
            db.NhaXuatBans.AddRange(nxbs);
            db.SaveChanges();

            // ---- Tài khoản nhân sự ----
            if (!db.TaiKhoans.Any(t => t.TenDangNhap == "admin2"))
                db.TaiKhoans.Add(new TaiKhoan { TenDangNhap = "admin2", MatKhau = "123456", HoTen = "Trần Quản Trị", Email = "admin2@thuvien.vn", VaiTro = 1, TrangThai = true });
            var thuThus = new List<TaiKhoan>
            {
                new() { TenDangNhap = "thuthu1", MatKhau = "123456", HoTen = "Nguyễn Thị Lan", Email = "lan@thuvien.vn", VaiTro = 2, TrangThai = true },
                new() { TenDangNhap = "thuthu2", MatKhau = "123456", HoTen = "Lê Văn Hùng", Email = "hung@thuvien.vn", VaiTro = 2, TrangThai = true },
                new() { TenDangNhap = "thuthu3", MatKhau = "123456", HoTen = "Phạm Thu Hà", Email = "ha@thuvien.vn", VaiTro = 2, TrangThai = false }, // tài khoản bị khóa để test
            };
            db.TaiKhoans.AddRange(thuThus);
            db.SaveChanges();
            var nhanVienIds = db.TaiKhoans.Where(t => t.VaiTro == 2 && t.TrangThai).Select(t => t.MaTaiKhoan).ToList();

            // ---- 15 sách (đa dạng trạng thái, giá, năm) ----
            string[] tenSach =
            {
                "Dế Mèn Phiêu Lưu Ký", "Nhà Giả Kim", "Sapiens: Lược Sử Loài Người", "Vũ Trụ Trong Vỏ Hạt Dẻ", "Số Đỏ",
                "Truyện Kiều", "Lịch Sử Việt Nam Bằng Tranh", "Đại Việt Sử Ký Toàn Thư", "Clean Code", "Lập Trình C# Cơ Bản",
                "ASP.NET Core Thực Chiến", "Cấu Trúc Dữ Liệu Và Giải Thuật", "Tôi Thấy Hoa Vàng Trên Cỏ Xanh", "Cosmos", "Chiến Tranh Và Hòa Bình"
            };
            int[] theLoaiIdx = { 0, 0, 3, 1, 2, 2, 3, 3, 4, 4, 4, 4, 0, 1, 0 };
            var sachs = new List<Sach>();
            for (int i = 0; i < tenSach.Length; i++)
            {
                int sl = rnd.Next(2, 9);
                sachs.Add(new Sach
                {
                    TenSach = tenSach[i],
                    MaTheLoai = theLoais[theLoaiIdx[i]].MaTheLoai,
                    MaNhaXuatBan = nxbs[i % nxbs.Count].MaNhaXuatBan,
                    NamXuatBan = 2005 + (i * 3) % 20,
                    SoLuong = sl,
                    SoLuongCon = sl, // sẽ trừ theo phiếu đang mượn bên dưới
                    DonGia = 50000 + (i * 17000) % 250000,
                    ViTriKeSach = $"K{i / 5 + 1}-T{i % 5 + 1}",
                    TrangThai = i != 13, // 1 cuốn ngưng cho mượn
                    GhiChu = i % 4 == 0 ? "Sách được mượn nhiều" : null
                });
            }
            db.Sachs.AddRange(sachs);
            db.SaveChanges();

            // ---- 30 bạn đọc + tài khoản ----
            string[] ho = { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô" };
            string[] dem = { "Văn", "Thị", "Minh", "Thu", "Quốc", "Ngọc" };
            string[] ten = { "An", "Bình", "Cường", "Dung", "Giang", "Hải", "Khánh", "Linh", "Nam", "Oanh", "Phúc", "Quân", "Sơn", "Trang", "Vy" };
            var banDocs = new List<BanDoc>();
            for (int i = 1; i <= 30; i++)
            {
                var hoTen = $"{ho[rnd.Next(ho.Length)]} {dem[rnd.Next(dem.Length)]} {ten[(i - 1) % ten.Length]}";
                DateTime ngayCap = today.AddMonths(-rnd.Next(2, 20));
                DateTime hanThe = ngayCap.AddYears(1);
                bool khoa = i == 29;
                if (i == 30 || i == 28) hanThe = today.AddDays(-rnd.Next(10, 60)); // 2 thẻ hết hạn
                if (i <= 24) hanThe = today.AddMonths(rnd.Next(2, 12));
                banDocs.Add(new BanDoc
                {
                    HoTen = hoTen,
                    NgaySinh = new DateTime(1998 + rnd.Next(0, 8), rnd.Next(1, 13), rnd.Next(1, 28)),
                    GioiTinh = i % 2 == 0,
                    SoDienThoai = $"09{rnd.Next(10000000, 99999999)}",
                    Email = $"bandoc{i:00}@mail.vn",
                    DiaChi = $"Số {rnd.Next(1, 200)} phố {ten[rnd.Next(ten.Length)]}, Hà Nội",
                    NgayCapThe = ngayCap,
                    HanThe = hanThe,
                    TrangThai = !khoa,
                    GhiChu = khoa ? "Thẻ bị khóa do vi phạm nội quy" : null,
                    TaiKhoan = new TaiKhoan
                    {
                        TenDangNhap = $"bandoc{i:00}",
                        MatKhau = "123456",
                        HoTen = hoTen,
                        Email = $"bandoc{i:00}@mail.vn",
                        VaiTro = 3,
                        TrangThai = !khoa
                    }
                });
            }
            db.BanDocs.AddRange(banDocs);
            db.SaveChanges();

            // ---- 45 phiếu mượn: 8 chờ duyệt, 10 đang mượn (4 quá hạn), 22 đã trả (một số trễ), 5 từ chối ----
            var activeBanDocs = banDocs.Where(b => b.TrangThai && b.HanThe >= today).ToList();
            var borrowedNow = new HashSet<(int, int)>(); // (bạn đọc, sách) đang giữ -> tránh trùng
            var stockAdjust = new Dictionary<int, int>();

            // Chuỗi trạng thái: 0 x8, 1 x10, 2 x22, 3 x5
            var plan = new List<int>();
            plan.AddRange(Enumerable.Repeat(2, 22));
            plan.AddRange(Enumerable.Repeat(1, 10));
            plan.AddRange(Enumerable.Repeat(0, 8));
            plan.AddRange(Enumerable.Repeat(3, 5));

            int overdueLeft = 4;
            foreach (var st in plan)
            {
                var bd = activeBanDocs[rnd.Next(activeBanDocs.Count)];
                var candidates = sachs.Where(s => s.TrangThai).OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 3)).ToList();
                if (st is 0 or 1) candidates = candidates.Where(s => !borrowedNow.Contains((bd.MaBanDoc, s.MaSach))).ToList();
                if (candidates.Count == 0) continue;

                DateTime ngayMuon, hanTra; DateTime? ngayTra = null; decimal phat = 0;
                switch (st)
                {
                    case 2:
                        ngayMuon = today.AddDays(-rnd.Next(20, 170));
                        hanTra = ngayMuon.Date.AddDays(14);
                        bool tre = rnd.Next(100) < 30;
                        ngayTra = tre ? hanTra.AddDays(rnd.Next(1, 8)).AddHours(10) : ngayMuon.AddDays(rnd.Next(2, 14)).AddHours(9);
                        break;
                    case 1:
                        if (overdueLeft > 0) { ngayMuon = today.AddDays(-rnd.Next(17, 30)); overdueLeft--; }
                        else ngayMuon = today.AddDays(-rnd.Next(0, 10));
                        hanTra = ngayMuon.Date.AddDays(14);
                        break;
                    default:
                        ngayMuon = today.AddDays(-rnd.Next(0, 4));
                        hanTra = today.AddDays(14);
                        break;
                }

                var pm = new PhieuMuon
                {
                    MaBanDoc = bd.MaBanDoc,
                    NgayMuon = ngayMuon.AddHours(8 + rnd.Next(0, 8)),
                    HanTra = hanTra,
                    NgayTra = ngayTra,
                    TrangThai = st,
                    NhanVienXuLy = st == 0 ? null : nhanVienIds[rnd.Next(nhanVienIds.Count)],
                    GhiChu = st == 3 ? "Từ chối: Sách đang được giữ chỗ." : st == 0 ? "Bạn đọc gửi yêu cầu mượn trực tuyến." : null
                };

                foreach (var s in candidates)
                {
                    var ct = new ChiTietMuon { MaSach = s.MaSach, SoLuong = 1, TinhTrangMuon = "Bình thường" };
                    if (st == 2)
                    {
                        int late = Math.Max(0, (ngayTra!.Value.Date - hanTra.Date).Days);
                        ct.TinhTrangTra = "Bình thường";
                        ct.SoNgayQuaHan = late;
                        ct.TienPhat = late * 5000m;
                        phat += ct.TienPhat;
                    }
                    pm.ChiTietMuons.Add(ct);
                    if (st == 1) { borrowedNow.Add((bd.MaBanDoc, s.MaSach)); stockAdjust[s.MaSach] = stockAdjust.GetValueOrDefault(s.MaSach) + 1; }
                    if (st == 0) borrowedNow.Add((bd.MaBanDoc, s.MaSach));
                }
                pm.TienPhat = phat;
                db.PhieuMuons.Add(pm);
            }

            foreach (var (maSach, n) in stockAdjust)
                sachs.First(s => s.MaSach == maSach).SoLuongCon -= n;

            // đảm bảo SoLuongCon không âm và 1 cuốn hết sách hoàn toàn để test
            foreach (var s in sachs) if (s.SoLuongCon < 0) { s.SoLuong += -s.SoLuongCon; s.SoLuongCon = 0; }
            // 1 đầu sách "Hết sách" hoàn toàn (tổng = số đang cho mượn) để test lọc còn/hết
            var hetSach = sachs[4];
            hetSach.SoLuong = stockAdjust.GetValueOrDefault(hetSach.MaSach);
            hetSach.SoLuongCon = 0;

            db.SaveChanges();
            tx.Commit();
        }
    }
}
