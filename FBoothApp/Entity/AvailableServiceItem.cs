using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Entity
{
    public class AvailableServiceItem
    {
        public string ServiceName { get; set; }
        public decimal ServicePrice { get; set; }
        public Guid ServiceID { get; set; }
        public int Quantity { get; set; }
    }
}
