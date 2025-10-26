using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    public class CuaHang
    {
        public string MaCH { get; set; }
        public string TenCH { get; set; }
        public string DiaChiCuThe { get; set; }
        public string SoDienThoai { get; set; }

        // Thuộc tính hiển thị cho Dropdown
        public string DisplayName => $"{TenCH} ({DiaChiCuThe})";
    }
}