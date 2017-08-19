using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DM106_final.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantidade { get; set; }
        //	Foreign	Key
        public int ProductId { get; set; }
        public int OrderId { get; set; }

        //	Navigation	property
        public	virtual	Product	Product	{ get;set; }

    }
}