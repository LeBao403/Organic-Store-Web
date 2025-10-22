using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    public class NguoiQuanTri : TaiKhoan
    {
        public string MaQT { get; set; }
        public string HoTen { get; set; }
    }
}