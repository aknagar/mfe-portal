using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AugmentService.Core.Entities
{
    public class Order
    {
        public string Name { get; set; }
        public int TotalCost { get; set; }
        public int Quantity { get; set; }
    }
}

