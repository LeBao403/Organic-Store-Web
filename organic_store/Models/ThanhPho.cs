using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    public class ThanhPho
    {
        public string MaTP { get; set; }
        public string TenTP { get; set; }
    }

    public class Quan
    {
        public string MaQuan { get; set; }
        public string TenQuan { get; set; }
    }

    public class Phuong
    {
        public string MaPhuong { get; set; }
        public string TenPhuong { get; set; }
    }
}