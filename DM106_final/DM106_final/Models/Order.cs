using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DM106_final.Models
{
    public class Order
    {
        public Order() { this.OrderItems = new HashSet<OrderItem>(); }
        public int Id { get; set; }
        public string userName { get; set; }
        public string dataPedido { get; set; }
        public string dataEntrega { get; set; }
        public string status { get; set; }//novo,fechado,cancelado,entregue
        public decimal precoTotalPedido { get; set; }
        public decimal pesoTotalPedido { get; set; }
        public decimal precoFrete { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}