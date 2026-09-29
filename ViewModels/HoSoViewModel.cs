using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.ViewModels
{
    // Bạn đọc chỉ sửa được các trường cá nhân; hạn thẻ, trạng thái... do thủ thư quản lý
    public class HoSoViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = "";

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Giới tính")]
        public bool? GioiTinh { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }
    }

    public class DoiMatKhauViewModel
    {
        [Required(ErrorMessage = "Nhập mật khẩu hiện tại")]
        public string MatKhauCu { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu mới")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu mới từ 6 ký tự")]
        public string MatKhauMoi { get; set; } = "";

        [Compare(nameof(MatKhauMoi), ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string XacNhan { get; set; } = "";
    }

    public class HoSoPageViewModel
    {
        public HoSoViewModel HoSo { get; set; } = new();
        public DoiMatKhauViewModel DoiMatKhau { get; set; } = new();
        public QuanLyThuVien.Models.BanDoc BanDoc { get; set; } = null!;
        public string TenDangNhap { get; set; } = "";
    }
}
