// organic_store.Models/Products.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    public class Products
    {
        public long Id { get; set; }
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string DonVi { get; set; }
        public double GiaBan { get; set; }
        public string MoTa { get; set; }
        public string HinhAnhURL { get; set; }

        // Quan hệ
        public string MaDM { get; set; } 

        // THUỘC TÍNH MỚI CHO LOGIC CỬA HÀNG
        public long SoTon { get; set; } // Số lượng tồn kho tại cửa hàng (lấy từ quan hệ CUNG_CAP)
        public string TenCH { get; set; } // Tên cửa hàng đang hiển thị (hoặc "Tất cả")

    }
}