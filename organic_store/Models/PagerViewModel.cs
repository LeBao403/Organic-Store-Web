using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    public class PagerViewModel
    {
        public int Page { get; set; }           // Trang hiện tại
        public int PageSize { get; set; }       // Số bản ghi/trang
        public int TotalCount { get; set; }     // Tổng số bản ghi
        public int DisplayPages { get; set; } = 5; // Số nút hiển thị cố định
        public string ActionName { get; set; } = "Index"; // Tên Action khi click
        public object RouteValues { get; set; } // RouteValues bổ sung (ví dụ: area = "Admin")
    }
}