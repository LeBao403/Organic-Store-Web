using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    public class KhachHang: TaiKhoan
    {
        public string MaKH { get; set; }

        public string HoTen { get; set; }
        public string DiaChiCuThe { get; set; }
        public DateTime NgaySinh { get; set; }
        public string LoaiKH { get; set; } = "thường";
    }
}