namespace QuanLyThuVien.ViewModels
{
    public class PagerViewModel
    {
        public string Action { get; set; } = "Index";
        public int Page { get; set; }
        public int TotalPages { get; set; }

        // Các query param cần giữ nguyên khi chuyển trang (search, filter, sort...)
        public Dictionary<string, string> Base { get; set; } = new();

        public Dictionary<string, string> Route(int page)
        {
            var d = new Dictionary<string, string>(Base) { ["pageNumber"] = page.ToString() };
            return d;
        }

        public static PagerViewModel Create(string action, int page, int totalPages, params (string Key, object? Value)[] keep)
        {
            var vm = new PagerViewModel { Action = action, Page = page, TotalPages = totalPages };
            foreach (var (k, v) in keep)
            {
                var text = v?.ToString();
                if (!string.IsNullOrEmpty(text)) vm.Base[k] = text;
            }
            return vm;
        }
    }
}
