using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    public class TempCartItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public double GiaBan { get; set; }
        public string HinhAnhURL { get; set; }
        public int Quantity { get; set; }
        public long SoTon { get; set; }
        public string MaCH { get; set; }
    }
}