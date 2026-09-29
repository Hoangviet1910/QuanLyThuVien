using QuanLyThuVien.Models;

namespace QuanLyThuVien.ViewModels
{
    public class TheLoaiThongKe
    {
        public string TenTheLoai { get; set; } = "";
        public int SoDauSach { get; set; }
        public int SoLuong { get; set; }
    }

    public class DashboardViewModel
    {
        // Thu thu / Admin
        public int TongSach { get; set; }
        public int SachKhaDung { get; set; }
        public int TongBanDoc { get; set; }
        public int PhieuChoDuyet { get; set; }
        public int PhieuDangMuon { get; set; }
        public int PhieuQuaHan { get; set; }
        public int TongTheLoai { get; set; }
        public int TongPhieu { get; set; }
        public int PhieuHoanThanh { get; set; }
        public decimal TongTienPhat { get; set; }
        public List<TheLoaiThongKe> ThongKeTheLoai { get; set; } = new();
        public List<PhieuMuon> PhieuMoiNhat { get; set; } = new();

        public int SachDangMuon => TongSach - SachKhaDung;
        public double TyLeDangMuon => TongSach == 0 ? 0 : Math.Round(SachDangMuon * 100.0 / TongSach, 1);

        // Ban doc
        public bool CoHoSoBanDoc { get; set; } = true;
        public BanDoc? BanDoc { get; set; }
        public int PhieuChoDuyetCuaToi { get; set; }
        public List<ChiTietMuon> SachDangMuonCuaToi { get; set; } = new();
    }
}
