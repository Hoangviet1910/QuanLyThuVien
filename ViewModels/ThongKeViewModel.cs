using QuanLyThuVien.Models;

namespace QuanLyThuVien.ViewModels
{
    public class ThangThongKe
    {
        public string Thang { get; set; } = "";
        public int SoPhieu { get; set; }
        public int SoCuon { get; set; }
    }

    public class TopItem
    {
        public string Ten { get; set; } = "";
        public int SoLuot { get; set; }
    }

    public class ThongKeViewModel
    {
        public int TongTheLoai { get; set; }
        public int TongDauSach { get; set; }
        public int TongBanDoc { get; set; }
        public int TongPhieu { get; set; }
        public int ChoXuLy { get; set; }
        public int DangXuLy { get; set; }
        public int HoanThanh { get; set; }
        public int HuyTuChoi { get; set; }

        public int QuaHanDangMuon { get; set; }   // đang mượn và đã quá hạn
        public int TraTre { get; set; }           // đã trả nhưng trễ hạn
        public int DaTra { get; set; }
        public int TraDungHan { get; set; }
        public double TyLeDungHan => DaTra == 0 ? 0 : Math.Round(TraDungHan * 100.0 / DaTra, 1);
        public decimal TongTienPhat { get; set; }

        public List<TheLoaiThongKe> SachTheoTheLoai { get; set; } = new();
        public List<ThangThongKe> LuotMuonTheoThang { get; set; } = new();
        public List<TopItem> TopSach { get; set; } = new();
        public List<TopItem> TopBanDoc { get; set; } = new();
    }
}
