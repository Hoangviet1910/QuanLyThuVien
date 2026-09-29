using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.ViewModels
{
    public class TaiKhoanFormViewModel
    {
        public int MaTaiKhoan { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, ErrorMessage = "Tối đa 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = "";

        // Bắt buộc khi tạo mới; để trống khi sửa = giữ nguyên
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 255 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string? MatKhau { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = "";

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Range(1, 3, ErrorMessage = "Vai trò không hợp lệ")]
        [Display(Name = "Vai trò")]
        public int VaiTro { get; set; } = 2;

        [Display(Name = "Đang hoạt động")]
        public bool TrangThai { get; set; } = true;

        // Chỉ để hiển thị
        public bool LaBanDoc { get; set; }
    }
}
