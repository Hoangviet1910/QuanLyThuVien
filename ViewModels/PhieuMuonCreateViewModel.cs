using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.ViewModels
{
    public class PhieuMuonCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn bạn đọc")]
        public int? MaBanDoc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn trả")]
        [DataType(DataType.Date)]
        public DateTime HanTra { get; set; } = DateTime.Today.AddDays(14);

        [StringLength(500)]
        public string? GhiChu { get; set; }

        public List<int> SelectedBooks { get; set; } = new();
    }
}
