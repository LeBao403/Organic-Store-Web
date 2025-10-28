using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace organic_store.Models
{
    [Serializable]
    public class CartItem
    {
        public Products Product { get; set; }
        public int Quantity { get; set; }

        public double Total => Product.GiaBan * Quantity;


    }
}