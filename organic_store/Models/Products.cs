using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
	public class Products
	{
        // ID nội bộ của Neo4j Node (ID(p))
        public long Id { get; set; }

        // Các thuộc tính ánh xạ từ Neo4j
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string DonVi { get; set; }
        public double GiaBan { get; set; }
        public string MoTa { get; set; }
        public string HinhAnhURL { get; set; }

        // Quan hệ
        public string MaDM { get; set; } // Thuộc danh mục nào

    }
}