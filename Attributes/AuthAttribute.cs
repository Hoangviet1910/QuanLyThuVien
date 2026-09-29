using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Attributes
{
    /// <summary>
    /// Kiểm tra đăng nhập (Session) + phân quyền theo vai trò tại Controller.
    /// Đồng thời kiểm tra lại DB: tài khoản bị khóa/xóa thì Session cũ mất hiệu lực.
    /// </summary>
    public class AuthAttribute : ActionFilterAttribute
    {
        public string? Roles { get; set; } // Comma separated roles: "1,2"

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var userId = http.Session.GetInt32("UserId");
            var userRole = http.Session.GetInt32("UserRole");
            var tempData = http.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http);

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "TaiKhoan", null);
                return;
            }

            // Tài khoản còn tồn tại, còn hoạt động và vai trò không đổi?
            var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
            var acc = db.TaiKhoans.AsNoTracking()
                .Where(t => t.MaTaiKhoan == userId)
                .Select(t => new { t.TrangThai, t.VaiTro })
                .FirstOrDefault();
            if (acc == null || !acc.TrangThai || acc.VaiTro != userRole)
            {
                http.Session.Clear();
                tempData["Error"] = "Phiên đăng nhập không còn hiệu lực (tài khoản bị khóa hoặc thay đổi). Vui lòng đăng nhập lại.";
                context.Result = new RedirectToActionResult("Login", "TaiKhoan", null);
                return;
            }

            if (!string.IsNullOrEmpty(Roles))
            {
                var allowedRoles = Roles.Split(',').Select(r => int.Parse(r.Trim())).ToList();
                if (!allowedRoles.Contains(userRole ?? 0))
                {
                    // Không dùng ForbidResult vì project không cấu hình Authentication scheme
                    tempData["Error"] = "Bạn không có quyền truy cập chức năng này.";
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
